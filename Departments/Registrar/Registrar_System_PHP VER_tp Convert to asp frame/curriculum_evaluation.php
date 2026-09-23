<?php
require 'config.php';

$displayName = $_SESSION['user_name'] ?? 'Registrar';
$registrarId = $_SESSION['user_id'] ?? 1;

$tab = $_GET['tab'] ?? 'audit';
if (!in_array($tab, ['audit', 'crediting', 'shifting'], true)) {
    $tab = 'audit';
}

$flash = get_flash();

// Handle Submissions
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $action = $_POST['action'] ?? '';

    try {
        // 1. Accredit Transferee Subject
        if ($action === 'accredit_subject') {
            $studentId  = (int)($_POST['student_id'] ?? 0);
            $school     = trim($_POST['prev_school'] ?? '');
            $prevCode   = strtoupper(trim($_POST['prev_subject_code'] ?? ''));
            $prevTitle  = trim($_POST['prev_subject_title'] ?? '');
            $units      = (float)($_POST['prev_units'] ?? 3.0);
            $grade      = trim($_POST['prev_grade'] ?? '1.50');
            $creditToId = (int)($_POST['credited_to_subject_id'] ?? 0);

            if ($studentId && $school && $prevCode && $creditToId) {
                $stmt = $pdo->prepare(
                    "INSERT INTO transferee_credited_subjects 
                     (student_id, prev_school, prev_subject_code, prev_subject_title, prev_units, prev_grade, credited_to_subject_id, status, evaluated_by, evaluated_at)
                     VALUES (?, ?, ?, ?, ?, ?, ?, 'approved', ?, NOW())"
                );
                $stmt->execute([$studentId, $school, $prevCode, $prevTitle, $units, $grade, $creditToId, $displayName]);

                // Update completed units
                $pdo->prepare("UPDATE student_profile SET completed_units = completed_units + ?, units_remaining = GREATEST(units_remaining - ?, 0) WHERE user_id = ?")
                    ->execute([(int)$units, (int)$units, $studentId]);

                log_activity($pdo, $studentId, "Transferee subject {$prevCode} accredited to institutional curriculum by {$displayName}.");
                set_flash("Transferee subject {$prevCode} accredited successfully.");
            } else {
                set_flash("Please fill in all crediting details.", 'error');
            }
            header("Location: curriculum_evaluation.php?tab=crediting");
            exit;
        }

        // 2. Approve / Reject Course Shifting
        if ($action === 'approve_shifting' || $action === 'reject_shifting') {
            $reqId = (int)($_POST['request_id'] ?? 0);
            $stmt = $pdo->prepare("SELECT * FROM course_shifting_requests WHERE id = ? AND status = 'pending'");
            $stmt->execute([$reqId]);
            $req = $stmt->fetch();

            if (!$req) {
                set_flash("Shifting request is no longer pending.", 'error');
            } elseif ($action === 'reject_shifting') {
                $pdo->prepare("UPDATE course_shifting_requests SET status = 'rejected', approved_at = NOW(), evaluated_by = ? WHERE id = ?")
                    ->execute([$displayName, $reqId]);
                log_activity($pdo, $req['student_id'], "Course shifting petition from {$req['from_program']} to {$req['to_program']} rejected by registrar.");
                set_flash("Course shifting petition rejected.");
            } else {
                $pdo->beginTransaction();
                // Update student profile program
                $pdo->prepare("UPDATE student_profile SET program = ? WHERE user_id = ?")
                    ->execute([$req['to_program'], $req['student_id']]);

                $pdo->prepare("UPDATE course_shifting_requests SET status = 'approved', approved_at = NOW(), evaluated_by = ? WHERE id = ?")
                    ->execute([$displayName, $reqId]);

                log_activity($pdo, $req['student_id'], "Course shifting approved: Program officially updated to {$req['to_program']} with credits migrated by {$displayName}.");
                $pdo->commit();
                set_flash("Course shifting approved. Student program updated to {$req['to_program']}.");
            }
            header("Location: curriculum_evaluation.php?tab=shifting");
            exit;
        }
    } catch (Throwable $e) {
        if ($pdo->inTransaction()) $pdo->rollBack();
        set_flash("Error: " . $e->getMessage(), 'error');
        header("Location: curriculum_evaluation.php?tab={$tab}");
        exit;
    }
}

// Students dropdown
$studentsList = $pdo->query("SELECT u.id, u.student_id_number, u.name, sp.program, sp.year_level FROM `user` u JOIN student_profile sp ON sp.user_id = u.id WHERE u.role = 'student' ORDER BY u.name ASC")->fetchAll();
$subjectsList = $pdo->query("SELECT id, subject_code, subject_name, units FROM subjects ORDER BY subject_code ASC")->fetchAll();

// Degree Audit for selected student
$selectedStudentId = isset($_GET['student_id']) ? (int)$_GET['student_id'] : ($studentsList[0]['id'] ?? 0);
$selectedStudent = null;
$curriculumProgress = [];
$auditStats = ['total' => 0, 'completed' => 0, 'in_progress' => 0, 'deficient' => 0];

if ($selectedStudentId > 0) {
    $stmt = $pdo->prepare("SELECT u.id, u.student_id_number, u.name, u.email, sp.* FROM `user` u JOIN student_profile sp ON sp.user_id = u.id WHERE u.id = ?");
    $stmt->execute([$selectedStudentId]);
    $selectedStudent = $stmt->fetch();

    if ($selectedStudent) {
        // Fetch all curriculum subjects
        $allSubs = $pdo->query("SELECT s.*, p.subject_code as prereq_code FROM subjects s LEFT JOIN subjects p ON p.id = s.prerequisite_subject_id ORDER BY s.year_level ASC, s.semester ASC, s.subject_code ASC")->fetchAll();

        // Fetch student's passed and enrolled subjects
        $stmtPassed = $pdo->prepare(
            "SELECT s.id as subject_id, g.grade, g.is_inc, es.status as enrollment_status
             FROM enrolled_subjects es
             JOIN enrollments e ON e.id = es.enrollment_id
             JOIN class_offerings co ON co.id = es.class_offering_id
             JOIN subjects s ON s.id = co.subject_id
             LEFT JOIN grades g ON g.enrolled_subject_id = es.id
             WHERE e.student_id = ?"
        );
        $stmtPassed->execute([$selectedStudentId]);
        $earnedMap = [];
        foreach ($stmtPassed->fetchAll() as $row) {
            $earnedMap[$row['subject_id']] = $row;
        }

        // Fetch accredited transferee subjects
        $stmtCredited = $pdo->prepare("SELECT credited_to_subject_id, prev_subject_code, prev_grade FROM transferee_credited_subjects WHERE student_id = ? AND status = 'approved'");
        $stmtCredited->execute([$selectedStudentId]);
        $creditedMap = [];
        foreach ($stmtCredited->fetchAll() as $row) {
            $creditedMap[$row['credited_to_subject_id']] = $row;
        }

        foreach ($allSubs as $sub) {
            $subId = $sub['id'];
            $status = 'Deficient';
            $mark = '—';
            $badge = 'badge-missing';

            if (isset($creditedMap[$subId])) {
                $status = 'Credited (Transferee)';
                $mark = $creditedMap[$subId]['prev_grade'];
                $badge = 'badge-approved';
                $auditStats['completed'] += (float)$sub['units'];
            } elseif (isset($earnedMap[$subId])) {
                $e = $earnedMap[$subId];
                if ($e['grade'] !== null && (float)$e['grade'] <= 3.00 && !$e['is_inc']) {
                    $status = 'Passed';
                    $mark = number_format((float)$e['grade'], 2);
                    $badge = 'badge-active';
                    $auditStats['completed'] += (float)$sub['units'];
                } elseif ($e['is_inc']) {
                    $status = 'Incomplete (INC)';
                    $mark = 'INC';
                    $badge = 'badge-inc';
                    $auditStats['in_progress'] += (float)$sub['units'];
                } else {
                    $status = 'Enrolled / In-Progress';
                    $mark = 'Enrolled';
                    $badge = 'badge-open';
                    $auditStats['in_progress'] += (float)$sub['units'];
                }
            } else {
                $auditStats['deficient'] += (float)$sub['units'];
            }

            $auditStats['total'] += (float)$sub['units'];

            $curriculumProgress[] = [
                'code'        => $sub['subject_code'],
                'name'        => $sub['subject_name'],
                'units'       => $sub['units'],
                'prereq'      => $sub['prereq_code'],
                'year_level'  => $sub['year_level'],
                'semester'    => $sub['semester'],
                'status'      => $status,
                'mark'        => $mark,
                'badge'       => $badge
            ];
        }
    }
}

// Transferee Credited Records
$creditedRecords = $pdo->query(
    "SELECT tcs.*, u.name as student_name, u.student_id_number, s.subject_code as credited_code, s.subject_name as credited_name
     FROM transferee_credited_subjects tcs
     JOIN `user` u ON u.id = tcs.student_id
     JOIN subjects s ON s.id = tcs.credited_to_subject_id
     ORDER BY tcs.evaluated_at DESC"
)->fetchAll();

// Course Shifting Requests
$shiftingRequests = $pdo->query(
    "SELECT csr.*, u.name as student_name, u.student_id_number, sp.year_level, sp.average_grade
     FROM course_shifting_requests csr
     JOIN `user` u ON u.id = csr.student_id
     JOIN student_profile sp ON sp.user_id = u.id
     ORDER BY csr.requested_at DESC"
)->fetchAll();
?>
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Curriculum Evaluation &amp; Crediting - Registrar</title>
  <link rel="stylesheet" href="dashboard.css">
</head>
<body class="dashboard-body">

  <?php render_sidebar('curriculum_evaluation.php'); ?>

  <div class="dash-main">
    <?php render_topbar($pdo, 'Curriculum Evaluation'); ?>

    <div class="dash-content">
      <?php if ($flash): ?>
        <div class="alert-banner alert-<?php echo htmlspecialchars($flash['type']); ?>">
          <span><?php echo htmlspecialchars($flash['message']); ?></span>
        </div>
      <?php endif; ?>

      <div class="dash-heading-row">
        <div>
          <h1 class="dash-heading">🧭 Curriculum Evaluation &amp; Degree Audit</h1>
          <p class="dash-subheading">Audit student flowchart completion, accredit transferee course equivalencies, and evaluate program shifts.</p>
        </div>
        <div>
          <?php if ($tab === 'crediting'): ?>
            <button class="btn btn-primary" onclick="document.getElementById('accreditModal').style.display='block'">➕ Accredit Transferee Subject</button>
          <?php endif; ?>
        </div>
      </div>

      <!-- Tab Navigation -->
      <div class="tab-nav">
        <a href="curriculum_evaluation.php?tab=audit" class="tab-link <?php echo $tab === 'audit' ? 'active' : ''; ?>">
          <span>📜 Degree Audit &amp; Flowchart Progress</span>
        </a>
        <a href="curriculum_evaluation.php?tab=crediting" class="tab-link <?php echo $tab === 'crediting' ? 'active' : ''; ?>">
          <span>🔄 Transferee Subject Crediting (<?php echo count($creditedRecords); ?>)</span>
        </a>
        <a href="curriculum_evaluation.php?tab=shifting" class="tab-link <?php echo $tab === 'shifting' ? 'active' : ''; ?>">
          <span>🔀 Course Shifting Petitions (<?php echo count($shiftingRequests); ?>)</span>
        </a>
      </div>

      <!-- TAB 1: DEGREE AUDIT & FLOWCHART -->
      <?php if ($tab === 'audit'): ?>
        <div class="dash-panel">
          <div class="dash-panel-header">
            <h2>Individual Student Degree Audit Flowchart</h2>
          </div>

          <form method="get" style="display:flex;gap:12px;align-items:center;margin-bottom:20px;">
            <input type="hidden" name="tab" value="audit">
            <label style="font-weight:700;font-size:13px;">Select Student:</label>
            <select name="student_id" onchange="this.form.submit()" style="max-width:380px;">
              <?php foreach ($studentsList as $s): ?>
                <option value="<?php echo $s['id']; ?>" <?php echo ($selectedStudentId === $s['id']) ? 'selected' : ''; ?>>
                  <?php echo htmlspecialchars($s['name'] . ' (' . $s['student_id_number'] . ') — ' . $s['program']); ?>
                </option>
              <?php endforeach; ?>
            </select>
          </form>

          <?php if ($selectedStudent): 
            $pctComplete = ($auditStats['total'] > 0) ? round(($auditStats['completed'] / $auditStats['total']) * 100, 1) : 0;
          ?>
            <div class="dash-panel" style="background:#f8fafc;border-color:#cbd5e1;margin-bottom:20px;">
              <div style="display:flex;justify-content:space-between;align-items:center;flex-wrap:wrap;gap:12px;">
                <div>
                  <h3 style="margin:0 0 4px;font-size:16px;"><?php echo htmlspecialchars($selectedStudent['name']); ?></h3>
                  <div style="font-size:12px;color:#64748b;">
                    <?php echo htmlspecialchars($selectedStudent['student_id_number']); ?> &middot; 
                    <?php echo htmlspecialchars($selectedStudent['program']); ?> &middot; 
                    Curriculum <?php echo htmlspecialchars($selectedStudent['curriculum_year']); ?>
                  </div>
                </div>
                <div style="text-align:right;">
                  <div style="font-size:22px;font-weight:800;color:var(--accent);"><?php echo $pctComplete; ?>% Completed</div>
                  <div style="font-size:12px;color:#64748b;"><?php echo $auditStats['completed']; ?> of <?php echo $auditStats['total']; ?> Units Earned</div>
                </div>
              </div>

              <!-- Progress bar -->
              <div style="background:#e2e8f0;border-radius:10px;height:12px;margin-top:14px;overflow:hidden;">
                <div style="width:<?php echo $pctComplete; ?>%;background:var(--accent);height:100%;border-radius:10px;"></div>
              </div>
            </div>

            <div class="table-responsive">
              <table class="table-simple">
                <thead>
                  <tr>
                    <th>Term / Year</th>
                    <th>Course Code</th>
                    <th>Descriptive Title</th>
                    <th>Units</th>
                    <th>Prerequisite</th>
                    <th>Grade Mark</th>
                    <th>Curriculum Status</th>
                  </tr>
                </thead>
                <tbody>
                  <?php foreach ($curriculumProgress as $cp): ?>
                  <tr>
                    <td><?php echo htmlspecialchars($cp['year_level'] . ' ' . $cp['semester']); ?></td>
                    <td><strong><?php echo htmlspecialchars($cp['code']); ?></strong></td>
                    <td><?php echo htmlspecialchars($cp['name']); ?></td>
                    <td><?php echo htmlspecialchars($cp['units']); ?>.0</td>
                    <td><?php echo $cp['prereq'] ? "<span class='badge badge-on-leave'>{$cp['prereq']}</span>" : '<span style="color:#94a3b8;">None</span>'; ?></td>
                    <td><strong><?php echo htmlspecialchars($cp['mark']); ?></strong></td>
                    <td><span class="badge <?php echo $cp['badge']; ?>"><?php echo htmlspecialchars($cp['status']); ?></span></td>
                  </tr>
                  <?php endforeach; ?>
                </tbody>
              </table>
            </div>
          <?php endif; ?>
        </div>

      <!-- TAB 2: TRANSFEREE SUBJECT CREDITING -->
      <?php elseif ($tab === 'crediting'): ?>
        <div class="dash-panel">
          <div class="dash-panel-header">
            <h2>Accredited Transferee Equivalent Courses</h2>
          </div>
          <p style="font-size:13px;color:#64748b;margin-top:0;">Evaluation and formal crediting of subjects earned from other tertiary institutions.</p>

          <?php if (empty($creditedRecords)): ?>
            <p style="text-align:center;padding:30px;color:#64748b;">No transferee credited subjects recorded.</p>
          <?php else: ?>
            <div class="table-responsive">
              <table class="table-simple">
                <thead>
                  <tr>
                    <th>Student Details</th>
                    <th>Originating Institution</th>
                    <th>Previous Course Details</th>
                    <th>Grade</th>
                    <th>Accredited MSU Subject</th>
                    <th>Evaluated By</th>
                    <th>Status</th>
                  </tr>
                </thead>
                <tbody>
                  <?php foreach ($creditedRecords as $cr): ?>
                  <tr>
                    <td>
                      <strong><?php echo htmlspecialchars($cr['student_name']); ?></strong>
                      <div style="font-size:11px;color:#64748b;"><?php echo htmlspecialchars($cr['student_id_number']); ?></div>
                    </td>
                    <td><?php echo htmlspecialchars($cr['prev_school']); ?></td>
                    <td>
                      <strong><?php echo htmlspecialchars($cr['prev_subject_code']); ?></strong>
                      <div style="font-size:11px;color:#64748b;"><?php echo htmlspecialchars($cr['prev_subject_title']); ?> (<?php echo $cr['prev_units']; ?> units)</div>
                    </td>
                    <td><strong><?php echo htmlspecialchars($cr['prev_grade']); ?></strong></td>
                    <td>
                      <strong style="color:var(--accent);"><?php echo htmlspecialchars($cr['credited_code']); ?></strong>
                      <div style="font-size:11px;color:#64748b;"><?php echo htmlspecialchars($cr['credited_name']); ?></div>
                    </td>
                    <td><?php echo htmlspecialchars($cr['evaluated_by']); ?></td>
                    <td><span class="badge badge-approved"><?php echo htmlspecialchars(ucfirst($cr['status'])); ?></span></td>
                  </tr>
                  <?php endforeach; ?>
                </tbody>
              </table>
            </div>
          <?php endif; ?>
        </div>

      <!-- TAB 3: COURSE SHIFTING PETITIONS -->
      <?php elseif ($tab === 'shifting'): ?>
        <div class="dash-panel">
          <div class="dash-panel-header">
            <h2>Degree Shifting Petitions &amp; Credit Migration</h2>
          </div>
          <p style="font-size:13px;color:#64748b;margin-top:0;">Approve program transfers and automatically align earned general education and major credits.</p>

          <?php if (empty($shiftingRequests)): ?>
            <p style="text-align:center;padding:30px;color:#64748b;">No course shifting petitions filed.</p>
          <?php else: ?>
            <div class="table-responsive">
              <table class="table-simple">
                <thead>
                  <tr>
                    <th>Student Details</th>
                    <th>Current Program</th>
                    <th>Target Program</th>
                    <th>GWA</th>
                    <th>Reason / Justification</th>
                    <th>Status</th>
                    <th style="text-align:right;">Action</th>
                  </tr>
                </thead>
                <tbody>
                  <?php foreach ($shiftingRequests as $sr): ?>
                  <tr>
                    <td>
                      <strong><?php echo htmlspecialchars($sr['student_name']); ?></strong>
                      <div style="font-size:11px;color:#64748b;"><?php echo htmlspecialchars($sr['student_id_number']); ?> &middot; <?php echo htmlspecialchars($sr['year_level']); ?></div>
                    </td>
                    <td><span class="badge badge-on-leave"><?php echo htmlspecialchars($sr['from_program']); ?></span></td>
                    <td><strong style="color:var(--accent);"><?php echo htmlspecialchars($sr['to_program']); ?></strong></td>
                    <td><?php echo $sr['average_grade'] ? number_format((float)$sr['average_grade'], 2) : '—'; ?></td>
                    <td style="font-size:12px;max-width:260px;color:#475569;"><?php echo htmlspecialchars($sr['reason']); ?></td>
                    <td><span class="badge badge-<?php echo strtolower($sr['status']); ?>"><?php echo htmlspecialchars(ucfirst($sr['status'])); ?></span></td>
                    <td style="text-align:right;">
                      <?php if ($sr['status'] === 'pending'): ?>
                        <form method="post" style="display:inline-flex;gap:6px;">
                          <input type="hidden" name="request_id" value="<?php echo $sr['id']; ?>">
                          <button type="submit" name="action" value="approve_shifting" class="btn-small primary">Approve &amp; Migrate</button>
                          <button type="submit" name="action" value="reject_shifting" class="btn-small danger">Reject</button>
                        </form>
                      <?php else: ?>
                        <span style="font-size:11px;color:#64748b;">Processed</span>
                      <?php endif; ?>
                    </td>
                  </tr>
                  <?php endforeach; ?>
                </tbody>
              </table>
            </div>
          <?php endif; ?>
        </div>
      <?php endif; ?>

    </div>
  </div>

  <!-- Modal: Accredit Transferee Subject -->
  <div id="accreditModal" style="display:none;position:fixed;top:0;left:0;right:0;bottom:0;background:rgba(15,23,42,0.6);z-index:999;">
    <div style="max-width:540px;background:#fff;border-radius:12px;margin:50px auto;padding:24px;box-shadow:0 20px 25px -5px rgba(0,0,0,0.2);max-height:90vh;overflow-y:auto;">
      <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px;">
        <h2 style="margin:0;font-size:18px;">Accredit Transferee Equivalent Course</h2>
        <button onclick="document.getElementById('accreditModal').style.display='none'" style="background:none;border:none;font-size:20px;cursor:pointer;">&times;</button>
      </div>
      <form method="post">
        <input type="hidden" name="action" value="accredit_subject">

        <div class="form-group" style="margin-bottom:12px;">
          <label>Select Transferee Student *</label>
          <select name="student_id" required>
            <option value="">-- Choose Student --</option>
            <?php foreach ($studentsList as $s): ?>
              <option value="<?php echo $s['id']; ?>"><?php echo htmlspecialchars($s['name'] . ' (' . $s['student_id_number'] . ')'); ?></option>
            <?php endforeach; ?>
          </select>
        </div>

        <div class="form-group" style="margin-bottom:12px;">
          <label>Originating School / University *</label>
          <input type="text" name="prev_school" placeholder="e.g. Far Eastern University" required>
        </div>

        <div class="form-grid">
          <div class="form-group">
            <label>Previous Course Code *</label>
            <input type="text" name="prev_subject_code" placeholder="e.g. CS101" required>
          </div>
          <div class="form-group">
            <label>Units Earned *</label>
            <input type="number" step="0.5" name="prev_units" value="3.0" required>
          </div>
        </div>

        <div class="form-grid">
          <div class="form-group">
            <label>Previous Course Title *</label>
            <input type="text" name="prev_subject_title" placeholder="e.g. Intro to Computing" required>
          </div>
          <div class="form-group">
            <label>Grade Earned *</label>
            <input type="text" name="prev_grade" placeholder="e.g. 1.50 or A" required>
          </div>
        </div>

        <div class="form-group" style="margin-bottom:20px;">
          <label>Accredit as MSU Institutional Subject *</label>
          <select name="credited_to_subject_id" required>
            <option value="">-- Choose Institutional Equivalent Subject --</option>
            <?php foreach ($subjectsList as $s): ?>
              <option value="<?php echo $s['id']; ?>"><?php echo htmlspecialchars($s['subject_code'] . ' — ' . $s['subject_name'] . ' (' . $s['units'] . ' units)'); ?></option>
            <?php endforeach; ?>
          </select>
        </div>

        <div style="display:flex;justify-content:flex-end;gap:10px;">
          <button type="button" class="btn btn-secondary" onclick="document.getElementById('accreditModal').style.display='none'">Cancel</button>
          <button type="submit" class="btn btn-primary">Accredit Subject</button>
        </div>
      </form>
    </div>
  </div>

</body>
</html>