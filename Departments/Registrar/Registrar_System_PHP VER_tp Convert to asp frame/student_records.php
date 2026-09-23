<?php
require 'config.php';

$displayName = $_SESSION['user_name'] ?? 'Registrar';

// Standard 201 File Checklist
const VAULT_DOCUMENT_TYPES = [
    'Form 137',
    'Form 138',
    'Birth Certificate',
    'Good Moral',
    'Transcript from Previous School',
    'Medical Clearance'
];

const ENROLLMENT_STATUSES = ['Active', 'On-Leave', 'Dropped', 'Dismissed', 'Graduated'];
const ACADEMIC_STATUSES = ['Good Standing', 'Probation', 'Graduating', 'Graduated', 'Disqualified'];

function ensure_vault_rows(PDO $pdo, int $studentId): void
{
    $stmt = $pdo->prepare('INSERT IGNORE INTO document_credentials (student_id, document_type) VALUES (?, ?)');
    foreach (VAULT_DOCUMENT_TYPES as $type) {
        $stmt->execute([$studentId, $type]);
    }
}

$flash = get_flash();
$viewId = isset($_GET['id']) ? (int) $_GET['id'] : 0;

// Handle Form Submissions
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $action = $_POST['action'] ?? '';
    $studentId = (int) ($_POST['student_id'] ?? 0);

    // Create New Student
    if ($action === 'create_student') {
        $idNumber = trim($_POST['student_id_number'] ?? '');
        $name = trim($_POST['name'] ?? '');
        $email = trim($_POST['email'] ?? '');
        $program = trim($_POST['program'] ?? 'BS Information Technology');
        $yearLevel = trim($_POST['year_level'] ?? '1st Year');
        $gender = trim($_POST['gender'] ?? 'Female');
        $contact = trim($_POST['contact_number'] ?? '');

        if ($idNumber && $name && $email) {
            try {
                $pdo->beginTransaction();
                $pwd = password_hash('password123', PASSWORD_DEFAULT);
                $stmt = $pdo->prepare("INSERT INTO `user` (student_id_number, name, email, password, role) VALUES (?, ?, ?, ?, 'student')");
                $stmt->execute([$idNumber, $name, $email, $pwd]);
                $newId = (int) $pdo->lastInsertId();

                $stmtP = $pdo->prepare("INSERT INTO student_profile (user_id, program, year_level, gender, contact_number, academic_status, enrollment_status) VALUES (?, ?, ?, ?, ?, 'Good Standing', 'Active')");
                $stmtP->execute([$newId, $program, $yearLevel, $gender, $contact]);

                ensure_vault_rows($pdo, $newId);
                log_activity($pdo, $newId, "New student record created: {$name} ({$idNumber}) by {$displayName}");
                $pdo->commit();

                set_flash("Student {$name} successfully created with ID {$idNumber}.");
                header("Location: student_records.php?id={$newId}");
                exit;
            } catch (Throwable $e) {
                if ($pdo->inTransaction()) $pdo->rollBack();
                set_flash("Error creating student: " . $e->getMessage(), 'error');
            }
        } else {
            set_flash("Please fill in all required student details.", 'error');
        }
        header("Location: student_records.php");
        exit;
    }

    // Update Academic & Enrollment Status
    if ($action === 'update_student_status' && $studentId > 0) {
        $enrStatus = $_POST['enrollment_status'] ?? 'Active';
        $acadStatus = $_POST['academic_status'] ?? 'Good Standing';
        $guidanceStatus = $_POST['guidance_clearance_status'] ?? 'Cleared';

        if (in_array($enrStatus, ENROLLMENT_STATUSES, true) && in_array($acadStatus, ACADEMIC_STATUSES, true)) {
            $stmt = $pdo->prepare('UPDATE student_profile SET enrollment_status = ?, academic_status = ?, guidance_clearance_status = ? WHERE user_id = ?');
            $stmt->execute([$enrStatus, $acadStatus, $guidanceStatus, $studentId]);
            log_activity($pdo, $studentId, "Student status updated: Enr={$enrStatus}, Acad={$acadStatus}, Guidance={$guidanceStatus} by {$displayName}");
            set_flash("Student status updated.");
        }
        header("Location: student_records.php?id={$studentId}");
        exit;
    }

    // Update 201 Document Credential
    if ($action === 'update_document' && $studentId > 0) {
        $docId = (int) ($_POST['doc_id'] ?? 0);
        $newStatus = $_POST['doc_status'] ?? 'Missing';
        $remarks = trim($_POST['remarks'] ?? '');

        if ($docId > 0 && in_array($newStatus, ['Missing', 'Submitted', 'Verified'], true)) {
            $now = date('Y-m-d H:i:s');
            if ($newStatus === 'Missing') {
                $stmt = $pdo->prepare('UPDATE document_credentials SET status = ?, remarks = ?, submitted_at = NULL, verified_at = NULL WHERE id = ? AND student_id = ?');
                $stmt->execute([$newStatus, $remarks, $docId, $studentId]);
            } elseif ($newStatus === 'Submitted') {
                $stmt = $pdo->prepare('UPDATE document_credentials SET status = ?, remarks = ?, submitted_at = COALESCE(submitted_at, ?), verified_at = NULL WHERE id = ? AND student_id = ?');
                $stmt->execute([$newStatus, $remarks, $now, $docId, $studentId]);
            } else { // Verified
                $stmt = $pdo->prepare('UPDATE document_credentials SET status = ?, remarks = ?, submitted_at = COALESCE(submitted_at, ?), verified_at = ? WHERE id = ? AND student_id = ?');
                $stmt->execute([$newStatus, $remarks, $now, $now, $docId, $studentId]);
            }
            log_activity($pdo, $studentId, "Document credential status updated to \"{$newStatus}\" by {$displayName}");
            set_flash("Document record updated.");
        }
        header("Location: student_records.php?id={$studentId}");
        exit;
    }
}

// ---- Detail View Logic ----
if ($viewId > 0) {
    $stmt = $pdo->prepare('SELECT id, student_id_number, name, email, created_at FROM `user` WHERE id = ? AND role = ?');
    $stmt->execute([$viewId, 'student']);
    $student = $stmt->fetch();

    if (!$student) {
        header('Location: student_records.php');
        exit;
    }

    ensure_vault_rows($pdo, $viewId);

    $stmt = $pdo->prepare('SELECT * FROM student_profile WHERE user_id = ?');
    $stmt->execute([$viewId]);
    $profile = $stmt->fetch() ?: [
        'program' => 'Not set', 'year_level' => '1st Year', 'average_grade' => null,
        'academic_status' => 'Good Standing', 'enrollment_status' => 'Active',
        'completed_units' => 0, 'units_remaining' => 144, 'guidance_clearance_status' => 'Cleared',
        'contact_number' => '', 'address' => '', 'gender' => 'Female'
    ];

    $stmt = $pdo->prepare('SELECT school_year, semester, enrollment_type, status, total_units, enrolled_at FROM enrollments WHERE student_id = ? ORDER BY enrolled_at DESC');
    $stmt->execute([$viewId]);
    $enrollmentHistory = $stmt->fetchAll();

    $stmt = $pdo->prepare('SELECT id, document_type, status, remarks, submitted_at, verified_at FROM document_credentials WHERE student_id = ? ORDER BY id ASC');
    $stmt->execute([$viewId]);
    $vault = $stmt->fetchAll();

    // Grades Ledger across all terms
    $stmt = $pdo->prepare("SELECT s.subject_code, s.subject_name, s.units, co.section_code, e.school_year, e.semester, g.grade, g.is_inc, g.status as grade_status, g.remarks
        FROM enrolled_subjects es
        JOIN enrollments e ON e.id = es.enrollment_id
        JOIN class_offerings co ON co.id = es.class_offering_id
        JOIN subjects s ON s.id = co.subject_id
        LEFT JOIN grades g ON g.enrolled_subject_id = es.id
        WHERE e.student_id = ?
        ORDER BY e.school_year DESC, e.semester DESC");
    $stmt->execute([$viewId]);
    $studentGrades = $stmt->fetchAll();

    // Transferee credited
    $stmt = $pdo->prepare("SELECT tcs.*, s.subject_code as institutional_code, s.subject_name as institutional_name 
        FROM transferee_credited_subjects tcs
        JOIN subjects s ON s.id = tcs.credited_to_subject_id
        WHERE tcs.student_id = ?");
    $stmt->execute([$viewId]);
    $creditedSubjects = $stmt->fetchAll();

    $stmt = $pdo->prepare('SELECT message, created_at FROM activity_log WHERE user_id = ? ORDER BY created_at DESC LIMIT 6');
    $stmt->execute([$viewId]);
    $activity = $stmt->fetchAll();
}

// ---- List View Logic ----
if ($viewId === 0) {
    $q = trim($_GET['q'] ?? '');
    $progFilter = trim($_GET['program'] ?? '');
    $statusFilter = trim($_GET['status'] ?? '');

    $sql = "SELECT u.id, u.student_id_number, u.name, u.email, sp.program, sp.year_level, sp.enrollment_status, sp.academic_status, sp.average_grade,
                   (SELECT COUNT(*) FROM document_credentials dc WHERE dc.student_id = u.id AND dc.status = 'Verified') AS verified_count,
                   (SELECT COUNT(*) FROM document_credentials dc WHERE dc.student_id = u.id) AS total_docs
            FROM `user` u
            LEFT JOIN student_profile sp ON sp.user_id = u.id
            WHERE u.role = 'student'";
    $params = [];

    if ($q !== '') {
        $sql .= " AND (u.name LIKE ? OR u.student_id_number LIKE ? OR u.email LIKE ?)";
        $params[] = "%$q%";
        $params[] = "%$q%";
        $params[] = "%$q%";
    }
    if ($progFilter !== '') {
        $sql .= " AND sp.program = ?";
        $params[] = $progFilter;
    }
    if ($statusFilter !== '') {
        $sql .= " AND sp.enrollment_status = ?";
        $params[] = $statusFilter;
    }

    $sql .= " ORDER BY u.name ASC";
    $stmt = $pdo->prepare($sql);
    $stmt->execute($params);
    $students = $stmt->fetchAll();

    $programsList = $pdo->query("SELECT DISTINCT program FROM student_profile WHERE program IS NOT NULL AND program != '' ORDER BY program")->fetchAll(PDO::FETCH_COLUMN);
}
?>
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Student Academic Records (201 Vault) - Registrar</title>
  <link rel="stylesheet" href="dashboard.css">
</head>
<body class="dashboard-body">

  <?php render_sidebar('student_records.php'); ?>

  <div class="dash-main">
    <?php render_topbar($pdo, 'Student Records (201 File)'); ?>

    <div class="dash-content">
      <?php if ($flash): ?>
        <div class="alert-banner alert-<?php echo htmlspecialchars($flash['type']); ?>">
          <span><?php echo htmlspecialchars($flash['message']); ?></span>
        </div>
      <?php endif; ?>

      <?php if ($viewId === 0): ?>
        <!-- LIST VIEW -->
        <div class="dash-heading-row">
          <div>
            <h1 class="dash-heading">🗂️ Student Academic Records (201 Vault)</h1>
            <p class="dash-subheading">Permanent records, academic standing, document credential tracking, and official clearances.</p>
          </div>
          <div>
            <button class="btn btn-primary" onclick="document.getElementById('newStudentModal').style.display='block'">➕ Register New Student</button>
          </div>
        </div>

        <div class="dash-panel">
          <form method="get" class="search-bar" style="flex-wrap:wrap;gap:12px;margin-bottom:20px;">
            <input type="text" name="q" placeholder="Search by name, ID number, or email..." value="<?php echo htmlspecialchars($q); ?>" style="flex:1;min-width:250px;">
            
            <select name="program" style="width:auto;min-width:200px;">
              <option value="">All Programs</option>
              <?php foreach ($programsList as $p): ?>
                <option value="<?php echo htmlspecialchars($p); ?>" <?php echo $progFilter === $p ? 'selected' : ''; ?>><?php echo htmlspecialchars($p); ?></option>
              <?php endforeach; ?>
            </select>

            <select name="status" style="width:auto;">
              <option value="">All Statuses</option>
              <?php foreach (ENROLLMENT_STATUSES as $st): ?>
                <option value="<?php echo htmlspecialchars($st); ?>" <?php echo $statusFilter === $st ? 'selected' : ''; ?>><?php echo htmlspecialchars($st); ?></option>
              <?php endforeach; ?>
            </select>

            <button type="submit" class="btn btn-secondary">Filter</button>
            <?php if ($q !== '' || $progFilter !== '' || $statusFilter !== ''): ?>
              <a href="student_records.php" class="btn btn-secondary">Clear</a>
            <?php endif; ?>
          </form>

          <?php if (!$students): ?>
            <div style="text-align:center;padding:40px;color:#64748b;">
              <p style="font-size:16px;margin:0;">No student records found matching the query.</p>
            </div>
          <?php else: ?>
            <div class="table-responsive">
              <table class="table-simple">
                <thead>
                  <tr>
                    <th>Student ID</th>
                    <th>Full Name</th>
                    <th>Program &amp; Year</th>
                    <th>GWA</th>
                    <th>Enrollment</th>
                    <th>201 Vault</th>
                    <th>Guidance</th>
                    <th>Action</th>
                  </tr>
                </thead>
                <tbody>
                  <?php foreach ($students as $s): 
                    $vaultRatio = ($s['total_docs'] > 0) ? "{$s['verified_count']}/{$s['total_docs']}" : "0/0";
                    $isComplete = ($s['total_docs'] > 0 && $s['verified_count'] === $s['total_docs']);
                  ?>
                  <tr>
                    <td><strong style="color:var(--accent);"><?php echo htmlspecialchars($s['student_id_number'] ?: 'NO-ID'); ?></strong></td>
                    <td>
                      <div style="font-weight:700;"><?php echo htmlspecialchars($s['name']); ?></div>
                      <div style="font-size:11px;color:#64748b;"><?php echo htmlspecialchars($s['email']); ?></div>
                    </td>
                    <td>
                      <div><?php echo htmlspecialchars($s['program'] ?? 'Not set'); ?></div>
                      <div style="font-size:11px;color:#64748b;"><?php echo htmlspecialchars($s['year_level'] ?? '1st Year'); ?></div>
                    </td>
                    <td><strong><?php echo $s['average_grade'] ? number_format((float)$s['average_grade'], 2) : '—'; ?></strong></td>
                    <td><span class="badge badge-<?php echo strtolower(str_replace([' ', '-'], '_', $s['enrollment_status'] ?? 'active')); ?>"><?php echo htmlspecialchars($s['enrollment_status'] ?? 'Active'); ?></span></td>
                    <td>
                      <span class="badge <?php echo $isComplete ? 'badge-verified' : 'badge-pending'; ?>">
                        <?php echo $vaultRatio; ?> verified
                      </span>
                    </td>
                    <td><span class="badge badge-<?php echo strtolower($s['academic_status'] ?? 'good_standing'); ?>"><?php echo htmlspecialchars($s['academic_status'] ?? 'Good Standing'); ?></span></td>
                    <td>
                      <a href="student_records.php?id=<?php echo $s['id']; ?>" class="btn-small primary">View 201 File ›</a>
                    </td>
                  </tr>
                  <?php endforeach; ?>
                </tbody>
              </table>
            </div>
          <?php endif; ?>
        </div>

      <?php else: ?>
        <!-- DETAIL VIEW -->
        <div style="margin-bottom:16px;" class="no-print">
          <a href="student_records.php" class="btn-small">‹ Back to Student Directory</a>
        </div>

        <!-- Student Profile Banner Header -->
        <div class="dash-panel" style="margin-bottom:20px;">
          <div style="display:flex;justify-content:space-between;align-items:flex-start;flex-wrap:wrap;gap:16px;">
            <div style="display:flex;gap:18px;align-items:center;">
              <div style="width:64px;height:64px;border-radius:12px;background:var(--accent-soft);color:var(--accent);display:flex;align-items:center;justify-content:center;font-size:28px;font-weight:bold;border:2px solid #bfdbfe;">
                🎓
              </div>
              <div>
                <h1 style="margin:0 0 4px;font-size:22px;color:var(--text-main);"><?php echo htmlspecialchars($student['name']); ?></h1>
                <div style="font-size:13px;color:#64748b;display:flex;gap:12px;align-items:center;flex-wrap:wrap;">
                  <span><strong>Student ID:</strong> <?php echo htmlspecialchars($student['student_id_number'] ?: 'N/A'); ?></span>
                  <span>&middot;</span>
                  <span><strong>Program:</strong> <?php echo htmlspecialchars($profile['program']); ?> (<?php echo htmlspecialchars($profile['year_level']); ?>)</span>
                  <span>&middot;</span>
                  <span><strong>Email:</strong> <?php echo htmlspecialchars($student['email']); ?></span>
                </div>
              </div>
            </div>

            <div style="display:flex;gap:8px;align-items:center;" class="no-print">
              <button onclick="window.print()" class="btn btn-secondary">🖨️ Print Permanent Record</button>
              <button onclick="document.getElementById('statusModal').style.display='block'" class="btn btn-primary">⚙️ Update Status</button>
            </div>
          </div>
        </div>

        <!-- Academic Metrics Row -->
        <div class="stat-mini-row" style="margin-bottom:20px;">
          <div class="stat-mini">
            <div class="label">General Weighted Average (GWA)</div>
            <div class="value" style="color:var(--accent);"><?php echo $profile['average_grade'] ? number_format((float)$profile['average_grade'], 2) : '1.25'; ?></div>
          </div>
          <div class="stat-mini">
            <div class="label">Curriculum Completed Units</div>
            <div class="value"><?php echo (int)$profile['completed_units']; ?> <span style="font-size:12px;color:#64748b;">/ 144 units</span></div>
          </div>
          <div class="stat-mini">
            <div class="label">Academic Standing</div>
            <div class="value"><span class="badge badge-<?php echo strtolower(str_replace(' ', '_', $profile['academic_status'])); ?>"><?php echo htmlspecialchars($profile['academic_status']); ?></span></div>
          </div>
          <div class="stat-mini">
            <div class="label">Enrollment Status</div>
            <div class="value"><span class="badge badge-<?php echo strtolower(str_replace([' ', '-'], '_', $profile['enrollment_status'])); ?>"><?php echo htmlspecialchars($profile['enrollment_status']); ?></span></div>
          </div>
          <div class="stat-mini">
            <div class="label">Guidance Clearance</div>
            <div class="value"><span class="badge badge-<?php echo strtolower($profile['guidance_clearance_status']); ?>"><?php echo htmlspecialchars($profile['guidance_clearance_status']); ?></span></div>
          </div>
        </div>

        <!-- 201 Credentials Vault -->
        <div class="dash-panel">
          <div class="dash-panel-header">
            <h2>🔐 201 Credentials Vault (Official Admission Requirements)</h2>
          </div>
          <p style="font-size:13px;color:#64748b;margin-top:0;">Official government documents required by CHED/DepEd for legal matriculation.</p>
          <div class="table-responsive">
            <table class="table-simple">
              <thead>
                <tr>
                  <th>Required Document</th>
                  <th>Status</th>
                  <th>Submitted At</th>
                  <th>Verified At</th>
                  <th>Remarks / Notes</th>
                  <th class="no-print">Action</th>
                </tr>
              </thead>
              <tbody>
                <?php foreach ($vault as $doc): ?>
                <tr>
                  <td><strong><?php echo htmlspecialchars($doc['document_type']); ?></strong></td>
                  <td><span class="badge badge-<?php echo strtolower($doc['status']); ?>"><?php echo htmlspecialchars($doc['status']); ?></span></td>
                  <td><?php echo $doc['submitted_at'] ? date('M d, Y', strtotime($doc['submitted_at'])) : '<span style="color:#94a3b8;">—</span>'; ?></td>
                  <td><?php echo $doc['verified_at'] ? date('M d, Y', strtotime($doc['verified_at'])) : '<span style="color:#94a3b8;">—</span>'; ?></td>
                  <td style="font-size:12px;color:#475569;"><?php echo htmlspecialchars($doc['remarks'] ?: 'No notes'); ?></td>
                  <td class="no-print">
                    <form method="post" style="display:flex;gap:6px;align-items:center;">
                      <input type="hidden" name="action" value="update_document">
                      <input type="hidden" name="student_id" value="<?php echo $student['id']; ?>">
                      <input type="hidden" name="doc_id" value="<?php echo $doc['id']; ?>">
                      <select name="doc_status" style="width:auto;padding:3px 6px;font-size:12px;">
                        <?php foreach (['Missing', 'Submitted', 'Verified'] as $st): ?>
                          <option value="<?php echo $st; ?>" <?php echo $doc['status'] === $st ? 'selected' : ''; ?>><?php echo $st; ?></option>
                        <?php endforeach; ?>
                      </select>
                      <input type="text" name="remarks" placeholder="Add remark..." value="<?php echo htmlspecialchars($doc['remarks'] ?? ''); ?>" style="width:140px;padding:3px 6px;font-size:12px;">
                      <button type="submit" class="btn-small primary">Save</button>
                    </form>
                  </td>
                </tr>
                <?php endforeach; ?>
              </tbody>
            </table>
          </div>
        </div>

        <!-- Academic Grades Ledger -->
        <div class="dash-panel">
          <div class="dash-panel-header">
            <h2>📜 Academic Grades Ledger &amp; Scholastic History</h2>
          </div>
          <?php if (!$studentGrades): ?>
            <p style="color:#64748b;font-size:13px;">No academic subjects evaluated yet for this student.</p>
          <?php else: ?>
            <div class="table-responsive">
              <table class="table-simple">
                <thead>
                  <tr>
                    <th>Term</th>
                    <th>Course Code</th>
                    <th>Descriptive Title</th>
                    <th>Units</th>
                    <th>Section</th>
                    <th>Final Grade</th>
                    <th>Remarks</th>
                  </tr>
                </thead>
                <tbody>
                  <?php foreach ($studentGrades as $g): 
                    $gradeStr = $g['is_inc'] ? 'INC' : ($g['grade'] !== null ? number_format((float)$g['grade'], 2) : '—');
                    $isPassed = ($g['grade'] !== null && (float)$g['grade'] <= 3.0);
                  ?>
                  <tr>
                    <td><?php echo htmlspecialchars($g['school_year'] . ' ' . $g['semester']); ?></td>
                    <td><strong><?php echo htmlspecialchars($g['subject_code']); ?></strong></td>
                    <td><?php echo htmlspecialchars($g['subject_name']); ?></td>
                    <td><?php echo htmlspecialchars($g['units']); ?></td>
                    <td><?php echo htmlspecialchars($g['section_code']); ?></td>
                    <td>
                      <strong style="color:<?php echo $isPassed ? 'var(--success)' : ($g['is_inc'] ? 'var(--purple)' : 'var(--danger)'); ?>;">
                        <?php echo htmlspecialchars($gradeStr); ?>
                      </strong>
                    </td>
                    <td><?php echo htmlspecialchars($g['remarks'] ?: 'In Progress'); ?></td>
                  </tr>
                  <?php endforeach; ?>
                </tbody>
              </table>
            </div>
          <?php endif; ?>
        </div>

        <!-- Transferee Credited Subjects (if any) -->
        <?php if (!empty($creditedSubjects)): ?>
        <div class="dash-panel">
          <div class="dash-panel-header">
            <h2>🔄 Transferee Accredited Subjects</h2>
          </div>
          <table class="table-simple">
            <thead>
              <tr>
                <th>Previous School</th>
                <th>Previous Course Code &amp; Title</th>
                <th>Units</th>
                <th>Grade</th>
                <th>Institutional Accredited Subject</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              <?php foreach ($creditedSubjects as $cs): ?>
              <tr>
                <td><?php echo htmlspecialchars($cs['prev_school']); ?></td>
                <td><strong><?php echo htmlspecialchars($cs['prev_subject_code']); ?></strong> - <?php echo htmlspecialchars($cs['prev_subject_title']); ?></td>
                <td><?php echo htmlspecialchars($cs['prev_units']); ?></td>
                <td><?php echo htmlspecialchars($cs['prev_grade']); ?></td>
                <td><strong style="color:var(--accent);"><?php echo htmlspecialchars($cs['institutional_code'] . ' - ' . $cs['institutional_name']); ?></strong></td>
                <td><span class="badge badge-approved"><?php echo htmlspecialchars(ucfirst($cs['status'])); ?></span></td>
              </tr>
              <?php endforeach; ?>
            </tbody>
          </table>
        </div>
        <?php endif; ?>

      <?php endif; ?>
    </div>
  </div>

  <!-- Register New Student Modal -->
  <div id="newStudentModal" style="display:none;position:fixed;top:0;left:0;right:0;bottom:0;background:rgba(15,23,42,0.6);z-index:999;">
    <div style="max-width:560px;background:#fff;border-radius:12px;margin:50px auto;padding:24px;box-shadow:0 20px 25px -5px rgba(0,0,0,0.2);max-height:90vh;overflow-y:auto;">
      <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px;">
        <h2 style="margin:0;font-size:18px;">Register New Student (201 Initializer)</h2>
        <button onclick="document.getElementById('newStudentModal').style.display='none'" style="background:none;border:none;font-size:20px;cursor:pointer;">&times;</button>
      </div>
      <form method="post">
        <input type="hidden" name="action" value="create_student">
        
        <div class="form-grid">
          <div class="form-group">
            <label>Student ID Number *</label>
            <input type="text" name="student_id_number" placeholder="e.g. 2025-00501" required>
          </div>
          <div class="form-group">
            <label>Full Legal Name *</label>
            <input type="text" name="name" placeholder="Last Name, First Name M.I." required>
          </div>
        </div>

        <div class="form-grid">
          <div class="form-group">
            <label>Email Address *</label>
            <input type="email" name="email" placeholder="student@msu.edu.ph" required>
          </div>
          <div class="form-group">
            <label>Contact Number</label>
            <input type="text" name="contact_number" placeholder="09xxxxxxxxx">
          </div>
        </div>

        <div class="form-grid">
          <div class="form-group">
            <label>Degree Program *</label>
            <select name="program">
              <option value="BS Information Technology">BS Information Technology</option>
              <option value="BS Computer Science">BS Computer Science</option>
              <option value="BS Business Administration">BS Business Administration</option>
              <option value="BS Office Administration">BS Office Administration</option>
            </select>
          </div>
          <div class="form-group">
            <label>Year Level</label>
            <select name="year_level">
              <option value="1st Year">1st Year</option>
              <option value="2nd Year">2nd Year</option>
              <option value="3rd Year">3rd Year</option>
              <option value="4th Year">4th Year</option>
            </select>
          </div>
        </div>

        <div class="form-group" style="margin-bottom:18px;">
          <label>Gender</label>
          <select name="gender">
            <option value="Female">Female</option>
            <option value="Male">Male</option>
          </select>
        </div>

        <div style="display:flex;justify-content:flex-end;gap:10px;">
          <button type="button" class="btn btn-secondary" onclick="document.getElementById('newStudentModal').style.display='none'">Cancel</button>
          <button type="submit" class="btn btn-primary">Create Record</button>
        </div>
      </form>
    </div>
  </div>

  <!-- Update Student Status Modal (for detail view) -->
  <?php if ($viewId > 0): ?>
  <div id="statusModal" style="display:none;position:fixed;top:0;left:0;right:0;bottom:0;background:rgba(15,23,42,0.6);z-index:999;">
    <div style="max-width:480px;background:#fff;border-radius:12px;margin:80px auto;padding:24px;box-shadow:0 20px 25px -5px rgba(0,0,0,0.2);">
      <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px;">
        <h2 style="margin:0;font-size:18px;">Update Student Official Status</h2>
        <button onclick="document.getElementById('statusModal').style.display='none'" style="background:none;border:none;font-size:20px;cursor:pointer;">&times;</button>
      </div>
      <form method="post">
        <input type="hidden" name="action" value="update_student_status">
        <input type="hidden" name="student_id" value="<?php echo $student['id']; ?>">

        <div class="form-group" style="margin-bottom:14px;">
          <label>Enrollment Status</label>
          <select name="enrollment_status">
            <?php foreach (ENROLLMENT_STATUSES as $st): ?>
              <option value="<?php echo $st; ?>" <?php echo $profile['enrollment_status'] === $st ? 'selected' : ''; ?>><?php echo $st; ?></option>
            <?php endforeach; ?>
          </select>
        </div>

        <div class="form-group" style="margin-bottom:14px;">
          <label>Academic Standing</label>
          <select name="academic_status">
            <?php foreach (ACADEMIC_STATUSES as $ast): ?>
              <option value="<?php echo $ast; ?>" <?php echo $profile['academic_status'] === $ast ? 'selected' : ''; ?>><?php echo $ast; ?></option>
            <?php endforeach; ?>
          </select>
        </div>

        <div class="form-group" style="margin-bottom:18px;">
          <label>Guidance Clearance Tag</label>
          <select name="guidance_clearance_status">
            <option value="Cleared" <?php echo ($profile['guidance_clearance_status'] ?? '') === 'Cleared' ? 'selected' : ''; ?>>Cleared</option>
            <option value="Pending" <?php echo ($profile['guidance_clearance_status'] ?? '') === 'Pending' ? 'selected' : ''; ?>>Pending</option>
            <option value="Flagged" <?php echo ($profile['guidance_clearance_status'] ?? '') === 'Flagged' ? 'selected' : ''; ?>>Flagged (Hold Status)</option>
          </select>
        </div>

        <div style="display:flex;justify-content:flex-end;gap:10px;">
          <button type="button" class="btn btn-secondary" onclick="document.getElementById('statusModal').style.display='none'">Cancel</button>
          <button type="submit" class="btn btn-primary">Save Changes</button>
        </div>
      </form>
    </div>
  </div>
  <?php endif; ?>

</body>
</html>