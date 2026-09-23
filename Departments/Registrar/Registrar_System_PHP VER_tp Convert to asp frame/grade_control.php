<?php
require 'config.php';

$currentSchoolYear = get_setting($pdo, 'current_school_year', '2025-2026');
$currentSemester   = get_setting($pdo, 'current_semester', '1st Semester');
$encodingOpen      = get_setting($pdo, 'grade_encoding_open', '1');
$encodingDeadline  = get_setting($pdo, 'grade_encoding_deadline', '2025-10-30');
$displayName       = $_SESSION['user_name'] ?? 'Registrar';
$registrarId       = $_SESSION['user_id'] ?? 1;

$tab = $_GET['tab'] ?? 'encoding';
if (!in_array($tab, ['encoding', 'verification', 'revisions'], true)) {
    $tab = 'encoding';
}

$flash = get_flash();

// Recalculate GWA for student
function recalculate_student_gwa(PDO $pdo, int $studentId): void
{
    $stmt = $pdo->prepare(
        "SELECT g.grade, s.units
         FROM enrolled_subjects es
         JOIN enrollments e ON e.id = es.enrollment_id
         JOIN class_offerings co ON co.id = es.class_offering_id
         JOIN subjects s ON s.id = co.subject_id
         JOIN grades g ON g.enrolled_subject_id = es.id
         WHERE e.student_id = ? AND g.status IN ('verified', 'locked') AND g.is_inc = 0 AND g.grade IS NOT NULL"
    );
    $stmt->execute([$studentId]);
    $rows = $stmt->fetchAll();

    if (!empty($rows)) {
        $totalQualityPoints = 0;
        $totalUnits = 0;
        foreach ($rows as $r) {
            $totalQualityPoints += ((float)$r['grade'] * (float)$r['units']);
            $totalUnits += (float)$r['units'];
        }
        $gwa = ($totalUnits > 0) ? round($totalQualityPoints / $totalUnits, 2) : null;
        $pdo->prepare("UPDATE student_profile SET average_grade = ? WHERE user_id = ?")->execute([$gwa, $studentId]);
    }
}

// Handle Form Actions
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $action = $_POST['action'] ?? '';

    try {
        // 1. Window Control
        if ($action === 'save_window') {
            $open = isset($_POST['window_open']) ? '1' : '0';
            $dead = trim($_POST['deadline'] ?? '');
            set_setting($pdo, 'grade_encoding_open', $open);
            set_setting($pdo, 'grade_encoding_deadline', $dead);
            log_activity($pdo, $registrarId, "Grade encoding window set to " . ($open === '1' ? 'OPEN' : 'CLOSED') . " by {$displayName}.");
            set_flash("Grade encoding window settings updated.");
            header("Location: grade_control.php?tab=encoding");
            exit;
        }

        // 2. Batch Save Section Grades
        if ($action === 'batch_save_grades') {
            if ($encodingOpen !== '1') {
                set_flash("Grade encoding window is currently CLOSED.", 'error');
            } else {
                $gradesData = $_POST['grades'] ?? [];
                $incData    = $_POST['is_inc'] ?? [];
                $offeringId = (int)($_POST['offering_id'] ?? 0);

                $pdo->beginTransaction();
                foreach ($gradesData as $enrolledSubId => $val) {
                    $enrolledSubId = (int)$enrolledSubId;
                    $isInc = isset($incData[$enrolledSubId]) ? 1 : 0;
                    $gradeVal = trim($val);

                    if ($isInc) {
                        $gradeToStore = null;
                        $remarks = 'Incomplete (INC)';
                    } elseif ($gradeVal !== '') {
                        $numGrade = (float)$gradeVal;
                        if ($numGrade < 1.00 || $numGrade > 5.00) continue;
                        $gradeToStore = $numGrade;
                        $remarks = ($numGrade <= 3.00) ? 'Passed' : 'Failed';
                    } else {
                        continue; // No entry
                    }

                    // Check existing
                    $check = $pdo->prepare("SELECT id, status FROM grades WHERE enrolled_subject_id = ?");
                    $check->execute([$enrolledSubId]);
                    $existing = $check->fetch();

                    if ($existing) {
                        if ($existing['status'] === 'locked') continue; // Do not touch locked grades
                        $upd = $pdo->prepare("UPDATE grades SET grade = ?, is_inc = ?, status = 'draft', remarks = ?, encoded_by = ?, encoded_at = NOW() WHERE id = ?");
                        $upd->execute([$gradeToStore, $isInc, $remarks, $registrarId, $existing['id']]);
                    } else {
                        $ins = $pdo->prepare("INSERT INTO grades (enrolled_subject_id, grade, is_inc, status, remarks, encoded_by, encoded_at) VALUES (?, ?, ?, 'draft', ?, ?, NOW())");
                        $ins->execute([$enrolledSubId, $gradeToStore, $isInc, $remarks, $registrarId]);
                    }
                }
                $pdo->commit();
                log_activity($pdo, $registrarId, "Grades encoded for offering #{$offeringId} by {$displayName}.");
                set_flash("Section grades saved as Draft.");
            }
            header("Location: grade_control.php?tab=encoding&offering_id=" . ($_POST['offering_id'] ?? ''));
            exit;
        }

        // 3. Verify & Lock Section Grades
        if ($action === 'verify_section' || $action === 'lock_section') {
            $offeringId = (int)($_POST['offering_id'] ?? 0);
            $newStatus = ($action === 'verify_section') ? 'verified' : 'locked';

            $stmt = $pdo->prepare(
                "SELECT g.id, e.student_id
                 FROM grades g
                 JOIN enrolled_subjects es ON es.id = g.enrolled_subject_id
                 JOIN enrollments e ON e.id = es.enrollment_id
                 WHERE es.class_offering_id = ?"
            );
            $stmt->execute([$offeringId]);
            $gradesToUpdate = $stmt->fetchAll();

            $pdo->beginTransaction();
            foreach ($gradesToUpdate as $gu) {
                if ($action === 'verify_section') {
                    $pdo->prepare("UPDATE grades SET status = 'verified', verified_by = ?, verified_at = NOW() WHERE id = ? AND status = 'draft'")
                        ->execute([$registrarId, $gu['id']]);
                } else {
                    $pdo->prepare("UPDATE grades SET status = 'locked', locked_at = NOW() WHERE id = ? AND status IN ('draft', 'verified')")
                        ->execute([$gu['id']]);
                }
                recalculate_student_gwa($pdo, (int)$gu['student_id']);
            }
            $pdo->commit();

            log_activity($pdo, $registrarId, "Grades {$newStatus} for class offering #{$offeringId} by {$displayName}.");
            set_flash("Class offering grades {$newStatus} and student GWAs recalculated.");
            header("Location: grade_control.php?tab=verification&offering_id={$offeringId}");
            exit;
        }

        // 4. Approve / Reject INC Completion or Revision
        if ($action === 'approve_revision' || $action === 'reject_revision') {
            $reqId = (int)($_POST['request_id'] ?? 0);
            $stmt = $pdo->prepare("SELECT * FROM completion_revision_requests WHERE id = ? AND status = 'pending'");
            $stmt->execute([$reqId]);
            $req = $stmt->fetch();

            if (!$req) {
                set_flash("Petition is no longer pending.", 'error');
            } elseif ($action === 'reject_revision') {
                $pdo->prepare("UPDATE completion_revision_requests SET status = 'rejected', processed_at = NOW(), processed_by = ? WHERE id = ?")
                    ->execute([$registrarId, $reqId]);
                log_activity($pdo, $req['student_id'], "Grade revision petition rejected by registrar.");
                set_flash("Grade revision petition rejected.");
            } else {
                $pdo->beginTransaction();
                $rem = ((float)$req['requested_grade'] <= 3.00) ? 'Passed (INC Completed)' : 'Failed';
                $pdo->prepare("UPDATE grades SET grade = ?, is_inc = 0, status = 'verified', remarks = ?, verified_by = ?, verified_at = NOW() WHERE id = ?")
                    ->execute([$req['requested_grade'], $rem, $registrarId, $req['grade_id']]);

                $pdo->prepare("UPDATE completion_revision_requests SET status = 'approved', processed_at = NOW(), processed_by = ? WHERE id = ?")
                    ->execute([$registrarId, $reqId]);

                recalculate_student_gwa($pdo, (int)$req['student_id']);
                $pdo->commit();

                log_activity($pdo, $req['student_id'], "Grade completion approved: final grade set to {$req['requested_grade']} by {$displayName}.");
                set_flash("Grade completion approved and applied to permanent record.");
            }
            header("Location: grade_control.php?tab=revisions");
            exit;
        }
    } catch (Throwable $e) {
        if ($pdo->inTransaction()) $pdo->rollBack();
        set_flash("Error processing grades: " . $e->getMessage(), 'error');
        header("Location: grade_control.php?tab={$tab}");
        exit;
    }
}

// Offerings list
$offeringsList = $pdo->query(
    "SELECT co.id, co.section_code, co.room, s.subject_code, s.subject_name, co.instructor_name
     FROM class_offerings co
     JOIN subjects s ON s.id = co.subject_id
     ORDER BY co.section_code ASC, s.subject_code ASC"
)->fetchAll();

$selectedOfferingId = isset($_GET['offering_id']) ? (int)$_GET['offering_id'] : ($offeringsList[0]['id'] ?? 0);

// Fetch students & grades for selected offering
$gradeSheet = [];
$selectedOffering = null;
if ($selectedOfferingId > 0) {
    $stmt = $pdo->prepare("SELECT co.*, s.subject_code, s.subject_name, s.units FROM class_offerings co JOIN subjects s ON s.id = co.subject_id WHERE co.id = ?");
    $stmt->execute([$selectedOfferingId]);
    $selectedOffering = $stmt->fetch();

    $stmtRoster = $pdo->prepare(
        "SELECT es.id as enrolled_sub_id, u.id as student_id, u.name, u.student_id_number, sp.program, sp.year_level,
                g.id as grade_id, g.grade, g.is_inc, g.status as grade_status, g.remarks
         FROM enrolled_subjects es
         JOIN enrollments e ON e.id = es.enrollment_id
         JOIN `user` u ON u.id = e.student_id
         LEFT JOIN student_profile sp ON sp.user_id = u.id
         LEFT JOIN grades g ON g.enrolled_subject_id = es.id
         WHERE es.class_offering_id = ? AND es.status = 'enrolled'
         ORDER BY u.name ASC"
    );
    $stmtRoster->execute([$selectedOfferingId]);
    $gradeSheet = $stmtRoster->fetchAll();
}

// Pending Revisions
$pendingRevisions = $pdo->query(
    "SELECT crr.*, u.name as student_name, u.student_id_number, s.subject_code, s.subject_name, g.grade as current_grade, g.is_inc as current_is_inc
     FROM completion_revision_requests crr
     JOIN `user` u ON u.id = crr.student_id
     JOIN grades g ON g.id = crr.grade_id
     JOIN enrolled_subjects es ON es.id = g.enrolled_subject_id
     JOIN class_offerings co ON co.id = es.class_offering_id
     JOIN subjects s ON s.id = co.subject_id
     WHERE crr.status = 'pending'
     ORDER BY crr.requested_at ASC"
)->fetchAll();
?>
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Grade Control &amp; Completion - Registrar</title>
  <link rel="stylesheet" href="dashboard.css">
</head>
<body class="dashboard-body">

  <?php render_sidebar('grade_control.php'); ?>

  <div class="dash-main">
    <?php render_topbar($pdo, 'Grade Control'); ?>

    <div class="dash-content">
      <?php if ($flash): ?>
        <div class="alert-banner alert-<?php echo htmlspecialchars($flash['type']); ?>">
          <span><?php echo htmlspecialchars($flash['message']); ?></span>
        </div>
      <?php endif; ?>

      <div class="dash-heading-row">
        <div>
          <h1 class="dash-heading">📊 Grade Control &amp; Verification Vault</h1>
          <p class="dash-subheading">Manage grade encoding periods, review instructor submissions, lock semester marks, and process INC completions.</p>
        </div>
        <div>
          <button class="btn btn-secondary" onclick="document.getElementById('windowModal').style.display='block'">⏰ Encoding Window: <?php echo ($encodingOpen === '1') ? '🟢 OPEN' : '🔴 CLOSED'; ?></button>
        </div>
      </div>

      <!-- Tab Navigation -->
      <div class="tab-nav">
        <a href="grade_control.php?tab=encoding" class="tab-link <?php echo $tab === 'encoding' ? 'active' : ''; ?>">
          <span>✏️ Section Grade Sheet Encoding</span>
        </a>
        <a href="grade_control.php?tab=verification" class="tab-link <?php echo $tab === 'verification' ? 'active' : ''; ?>">
          <span>🔒 Grade Verification &amp; Final Locking</span>
        </a>
        <a href="grade_control.php?tab=revisions" class="tab-link <?php echo $tab === 'revisions' ? 'active' : ''; ?>">
          <span>📝 INC Completion &amp; Revisions (<?php echo count($pendingRevisions); ?>)</span>
        </a>
      </div>

      <!-- TAB 1: SECTION GRADE ENCODING -->
      <?php if ($tab === 'encoding'): ?>
        <div class="dash-panel">
          <div class="dash-panel-header">
            <h2>Section Grade Encoding Sheet</h2>
            <?php if ($encodingOpen !== '1'): ?>
              <span class="badge badge-missing">Encoding Window Closed</span>
            <?php else: ?>
              <span class="badge badge-active">Deadline: <?php echo htmlspecialchars($encodingDeadline); ?></span>
            <?php endif; ?>
          </div>

          <form method="get" style="display:flex;gap:12px;align-items:center;margin-bottom:20px;">
            <input type="hidden" name="tab" value="encoding">
            <label style="font-weight:700;font-size:13px;">Class Offering Section:</label>
            <select name="offering_id" onchange="this.form.submit()" style="max-width:400px;">
              <?php foreach ($offeringsList as $o): ?>
                <option value="<?php echo $o['id']; ?>" <?php echo ($selectedOfferingId === $o['id']) ? 'selected' : ''; ?>>
                  <?php echo htmlspecialchars($o['section_code'] . ' — ' . $o['subject_code'] . ': ' . $o['subject_name']); ?>
                </option>
              <?php endforeach; ?>
            </select>
          </form>

          <?php if ($selectedOffering): ?>
            <div class="stat-mini-row" style="margin-bottom:16px;">
              <div class="stat-mini"><div class="label">Section</div><div class="value"><?php echo htmlspecialchars($selectedOffering['section_code']); ?></div></div>
              <div class="stat-mini"><div class="label">Course</div><div class="value"><?php echo htmlspecialchars($selectedOffering['subject_code']); ?></div></div>
              <div class="stat-mini"><div class="label">Units</div><div class="value"><?php echo htmlspecialchars($selectedOffering['units']); ?>.0</div></div>
              <div class="stat-mini"><div class="label">Instructor</div><div class="value" style="font-size:13px;"><?php echo htmlspecialchars($selectedOffering['instructor_name']); ?></div></div>
            </div>

            <?php if (empty($gradeSheet)): ?>
              <p style="text-align:center;padding:30px;color:#64748b;">No students currently enrolled in this section.</p>
            <?php else: ?>
              <form method="post">
                <input type="hidden" name="action" value="batch_save_grades">
                <input type="hidden" name="offering_id" value="<?php echo $selectedOffering['id']; ?>">

                <div class="table-responsive">
                  <table class="table-simple">
                    <thead>
                      <tr>
                        <th>#</th>
                        <th>Student ID</th>
                        <th>Student Full Name</th>
                        <th>Program</th>
                        <th>Grade (1.00 - 5.00)</th>
                        <th>Incomplete (INC)?</th>
                        <th>Status</th>
                        <th>Remarks</th>
                      </tr>
                    </thead>
                    <tbody>
                      <?php foreach ($gradeSheet as $idx => $row): 
                        $isLocked = ($row['grade_status'] === 'locked');
                      ?>
                      <tr>
                        <td><?php echo $idx + 1; ?></td>
                        <td><strong style="color:var(--accent);"><?php echo htmlspecialchars($row['student_id_number']); ?></strong></td>
                        <td><strong><?php echo htmlspecialchars($row['name']); ?></strong></td>
                        <td><?php echo htmlspecialchars($row['program']); ?></td>
                        <td style="width:140px;">
                          <?php if ($isLocked): ?>
                            <strong><?php echo $row['is_inc'] ? 'INC' : number_format((float)$row['grade'], 2); ?></strong>
                          <?php else: ?>
                            <input type="number" step="0.25" min="1.00" max="5.00" 
                                   name="grades[<?php echo $row['enrolled_sub_id']; ?>]" 
                                   value="<?php echo $row['grade'] !== null ? number_format((float)$row['grade'], 2) : ''; ?>"
                                   placeholder="1.00 - 5.00" 
                                   style="width:110px;"
                                   <?php echo ($encodingOpen !== '1') ? 'disabled' : ''; ?>>
                          <?php endif; ?>
                        </td>
                        <td style="width:120px;text-align:center;">
                          <?php if ($isLocked): ?>
                            <?php echo $row['is_inc'] ? '✔️ INC' : '—'; ?>
                          <?php else: ?>
                            <label style="font-weight:normal;font-size:12px;display:inline-flex;align-items:center;gap:4px;">
                              <input type="checkbox" name="is_inc[<?php echo $row['enrolled_sub_id']; ?>]" value="1" <?php echo $row['is_inc'] ? 'checked' : ''; ?> <?php echo ($encodingOpen !== '1') ? 'disabled' : ''; ?>> INC
                            </label>
                          <?php endif; ?>
                        </td>
                        <td>
                          <span class="badge badge-<?php echo strtolower($row['grade_status'] ?: 'draft'); ?>">
                            <?php echo htmlspecialchars(ucfirst($row['grade_status'] ?: 'Unencoded')); ?>
                          </span>
                        </td>
                        <td style="font-size:12px;color:#64748b;"><?php echo htmlspecialchars($row['remarks'] ?: '—'); ?></td>
                      </tr>
                      <?php endforeach; ?>
                    </tbody>
                  </table>
                </div>

                <?php if ($encodingOpen === '1'): ?>
                  <div style="margin-top:20px;display:flex;justify-content:flex-end;">
                    <button type="submit" class="btn btn-primary">💾 Save Draft Grades</button>
                  </div>
                <?php endif; ?>
              </form>
            <?php endif; ?>
          <?php endif; ?>
        </div>

      <!-- TAB 2: GRADE VERIFICATION & FINAL LOCKING -->
      <?php elseif ($tab === 'verification'): ?>
        <div class="dash-panel">
          <div class="dash-panel-header">
            <h2>Registrar Grade Verification &amp; Final Locking</h2>
          </div>
          <p style="font-size:13px;color:#64748b;margin-top:0;">
            Once verified and locked, grades become immutable scholastic records. Student General Weighted Averages (GWA) are immediately calculated and permanent transcripts are finalized.
          </p>

          <form method="get" style="display:flex;gap:12px;align-items:center;margin-bottom:20px;">
            <input type="hidden" name="tab" value="verification">
            <label style="font-weight:700;font-size:13px;">Select Section:</label>
            <select name="offering_id" onchange="this.form.submit()" style="max-width:400px;">
              <?php foreach ($offeringsList as $o): ?>
                <option value="<?php echo $o['id']; ?>" <?php echo ($selectedOfferingId === $o['id']) ? 'selected' : ''; ?>>
                  <?php echo htmlspecialchars($o['section_code'] . ' — ' . $o['subject_code'] . ': ' . $o['subject_name']); ?>
                </option>
              <?php endforeach; ?>
            </select>
          </form>

          <?php if (!empty($gradeSheet)): ?>
            <div class="table-responsive">
              <table class="table-simple">
                <thead>
                  <tr>
                    <th>Student ID</th>
                    <th>Name</th>
                    <th>Grade</th>
                    <th>Remarks</th>
                    <th>Lifecycle Status</th>
                  </tr>
                </thead>
                <tbody>
                  <?php foreach ($gradeSheet as $row): 
                    $gradeDisplay = $row['is_inc'] ? 'INC' : ($row['grade'] !== null ? number_format((float)$row['grade'], 2) : 'No Grade');
                  ?>
                  <tr>
                    <td><strong><?php echo htmlspecialchars($row['student_id_number']); ?></strong></td>
                    <td><?php echo htmlspecialchars($row['name']); ?></td>
                    <td><strong><?php echo htmlspecialchars($gradeDisplay); ?></strong></td>
                    <td><?php echo htmlspecialchars($row['remarks'] ?: 'In Progress'); ?></td>
                    <td><span class="badge badge-<?php echo strtolower($row['grade_status'] ?: 'draft'); ?>"><?php echo htmlspecialchars(ucfirst($row['grade_status'] ?: 'Draft')); ?></span></td>
                  </tr>
                  <?php endforeach; ?>
                </tbody>
              </table>
            </div>

            <div style="margin-top:20px;display:flex;justify-content:flex-end;gap:12px;">
              <form method="post" style="display:inline;">
                <input type="hidden" name="offering_id" value="<?php echo $selectedOffering['id']; ?>">
                <button type="submit" name="action" value="verify_section" class="btn btn-secondary">✔️ Verify All Draft Grades</button>
                <button type="submit" name="action" value="lock_section" class="btn btn-primary" onclick="return confirm('Lock final section grades? Locked grades cannot be edited without formal board petition.')">🔒 Lock &amp; Calculate Final GWA</button>
              </form>
            </div>
          <?php endif; ?>
        </div>

      <!-- TAB 3: INC COMPLETION & REVISION PETITIONS -->
      <?php elseif ($tab === 'revisions'): ?>
        <div class="dash-panel">
          <div class="dash-panel-header">
            <h2>INC Removal &amp; Official Grade Revision Petitions</h2>
          </div>
          <p style="font-size:13px;color:#64748b;margin-top:0;">Students with Incomplete (INC) marks within the 1-year prescriptive period or faculty grading revisions.</p>

          <?php if (empty($pendingRevisions)): ?>
            <p style="text-align:center;padding:30px;color:#64748b;">No pending INC completion or revision petitions.</p>
          <?php else: ?>
            <div class="table-responsive">
              <table class="table-simple">
                <thead>
                  <tr>
                    <th>Student</th>
                    <th>Course Subject</th>
                    <th>Current Mark</th>
                    <th>Completed / Target Grade</th>
                    <th>Faculty Justification</th>
                    <th>Submitted</th>
                    <th style="text-align:right;">Action</th>
                  </tr>
                </thead>
                <tbody>
                  <?php foreach ($pendingRevisions as $pr): ?>
                  <tr>
                    <td>
                      <strong><?php echo htmlspecialchars($pr['student_name']); ?></strong>
                      <div style="font-size:11px;color:#64748b;"><?php echo htmlspecialchars($pr['student_id_number']); ?></div>
                    </td>
                    <td><strong><?php echo htmlspecialchars($pr['subject_code']); ?></strong> - <?php echo htmlspecialchars($pr['subject_name']); ?></td>
                    <td><span class="badge badge-inc"><?php echo $pr['current_is_inc'] ? 'INC' : number_format((float)$pr['current_grade'], 2); ?></span></td>
                    <td><strong style="color:var(--success);font-size:14px;"><?php echo number_format((float)$pr['requested_grade'], 2); ?></strong></td>
                    <td style="font-size:12px;max-width:260px;color:#475569;"><?php echo htmlspecialchars($pr['reason']); ?></td>
                    <td><?php echo date('M d, Y', strtotime($pr['requested_at'])); ?></td>
                    <td style="text-align:right;">
                      <form method="post" style="display:inline-flex;gap:6px;">
                        <input type="hidden" name="request_id" value="<?php echo $pr['id']; ?>">
                        <button type="submit" name="action" value="approve_revision" class="btn-small primary">Approve &amp; Finalize</button>
                        <button type="submit" name="action" value="reject_revision" class="btn-small danger">Reject</button>
                      </form>
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

  <!-- Modal: Grade Encoding Window Settings -->
  <div id="windowModal" style="display:none;position:fixed;top:0;left:0;right:0;bottom:0;background:rgba(15,23,42,0.6);z-index:999;">
    <div style="max-width:440px;background:#fff;border-radius:12px;margin:80px auto;padding:24px;box-shadow:0 20px 25px -5px rgba(0,0,0,0.2);">
      <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px;">
        <h2 style="margin:0;font-size:18px;">Grade Encoding Period Control</h2>
        <button onclick="document.getElementById('windowModal').style.display='none'" style="background:none;border:none;font-size:20px;cursor:pointer;">&times;</button>
      </div>
      <form method="post">
        <input type="hidden" name="action" value="save_window">

        <div class="form-group" style="margin-bottom:16px;">
          <label style="display:flex;align-items:center;gap:8px;font-size:14px;">
            <input type="checkbox" name="window_open" value="1" <?php echo $encodingOpen === '1' ? 'checked' : ''; ?>>
            Open Grade Encoding Window
          </label>
        </div>

        <div class="form-group" style="margin-bottom:20px;">
          <label>Encoding Deadline Date</label>
          <input type="date" name="deadline" value="<?php echo htmlspecialchars($encodingDeadline); ?>">
        </div>

        <div style="display:flex;justify-content:flex-end;gap:10px;">
          <button type="button" class="btn btn-secondary" onclick="document.getElementById('windowModal').style.display='none'">Cancel</button>
          <button type="submit" class="btn btn-primary">Save Changes</button>
        </div>
      </form>
    </div>
  </div>

</body>
</html>