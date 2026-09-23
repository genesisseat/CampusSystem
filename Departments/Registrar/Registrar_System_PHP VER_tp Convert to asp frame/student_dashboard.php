<?php
require 'config.php';

// Set to student role or default to Alyssa Mendoza
require_role('student');

$userId = (int) ($_SESSION['user_id'] ?? 5);

// Fetch Student Profile
$stmt = $pdo->prepare(
    "SELECT u.id, u.student_id_number, u.name, u.email, sp.*
     FROM `user` u
     JOIN student_profile sp ON sp.user_id = u.id
     WHERE u.id = ?"
);
$stmt->execute([$userId]);
$student = $stmt->fetch();

if (!$student) {
    // Fallback to first student if session id not found
    $firstStud = $pdo->query("SELECT id FROM `user` WHERE role = 'student' LIMIT 1")->fetchColumn();
    $userId = (int)$firstStud;
    $stmt->execute([$userId]);
    $student = $stmt->fetch();
}

$currentSchoolYear = get_setting($pdo, 'current_school_year', '2025-2026');
$currentSemester   = get_setting($pdo, 'current_semester', '1st Semester');

$flash = get_flash();

// Handle Document Request Filing from Student Portal
if ($_SERVER['REQUEST_METHOD'] === 'POST' && isset($_POST['action']) && $_POST['action'] === 'request_document') {
    $docType = trim($_POST['document_type'] ?? '');
    $purpose = trim($_POST['purpose'] ?? 'Personal requirement');
    $copies  = max(1, (int)($_POST['copies'] ?? 1));

    if ($docType !== '') {
        $prefix = strtoupper(substr(preg_replace('/[^A-Za-z]/', '', $docType), 0, 3));
        $qrToken = 'MSU-' . $prefix . '-' . date('Y') . '-' . strtoupper(bin2hex(random_bytes(3)));

        $stmt = $pdo->prepare(
            "INSERT INTO transcript_requests 
             (student_id, document_type, purpose, copies, status, qr_code_token, requested_at)
             VALUES (?, ?, ?, ?, 'pending', ?, NOW())"
        );
        $stmt->execute([$userId, $docType, $purpose, $copies, $qrToken]);
        log_activity($pdo, $userId, "Filed document request for {$docType} via Student Portal (Token: {$qrToken})");

        set_flash("Document request submitted! Tracking token: {$qrToken}");
    }
    header('Location: student_dashboard.php');
    exit;
}

// Current Schedule
$stmtSched = $pdo->prepare(
    "SELECT co.section_code, s.subject_code, s.subject_name, s.units, co.room,
            co.days_of_week, co.start_time, co.end_time, co.instructor_name
     FROM enrolled_subjects es
     JOIN enrollments e ON e.id = es.enrollment_id
     JOIN class_offerings co ON co.id = es.class_offering_id
     JOIN subjects s ON s.id = co.subject_id
     WHERE e.student_id = ? AND e.school_year = ? AND e.semester = ? AND es.status = 'enrolled'
     ORDER BY co.start_time ASC"
);
$stmtSched->execute([$userId, $currentSchoolYear, $currentSemester]);
$schedule = $stmtSched->fetchAll();

// Grades Ledger
$stmtGrades = $pdo->prepare(
    "SELECT e.school_year, e.semester, s.subject_code, s.subject_name, s.units,
            g.grade, g.is_inc, g.status as grade_status, g.remarks
     FROM enrolled_subjects es
     JOIN enrollments e ON e.id = es.enrollment_id
     JOIN class_offerings co ON co.id = es.class_offering_id
     JOIN subjects s ON s.id = co.subject_id
     LEFT JOIN grades g ON g.enrolled_subject_id = es.id
     WHERE e.student_id = ?
     ORDER BY e.school_year DESC, e.semester DESC, s.subject_code ASC"
);
$stmtGrades->execute([$userId]);
$gradesList = $stmtGrades->fetchAll();

// My Document Requests
$stmtDocs = $pdo->prepare("SELECT * FROM transcript_requests WHERE student_id = ? ORDER BY requested_at DESC");
$stmtDocs->execute([$userId]);
$myRequests = $stmtDocs->fetchAll();
?>
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Student Self-Service Portal - MSU</title>
  <link rel="stylesheet" href="dashboard.css">
</head>
<body class="dashboard-body">

  <aside class="dash-sidebar">
    <div class="dash-brand">
      <span class="cap-logo">🎓</span>
      <div class="brand-text">
        <div class="brand-title">STUDENT PORTAL</div>
        <div class="brand-sub">Metropolitan State Univ.</div>
      </div>
    </div>

    <nav class="dash-nav">
      <div class="nav-section-title">MY ACADEMIC SERVICES</div>
      <a href="student_dashboard.php" class="active"><span class="ic">🏠</span> Dashboard</a>
      <a href="#" onclick="document.getElementById('schedule-sec').scrollIntoView({behavior:'smooth'});return false;"><span class="ic">📅</span> Class Schedule</a>
      <a href="#" onclick="document.getElementById('grades-sec').scrollIntoView({behavior:'smooth'});return false;"><span class="ic">📜</span> Scholastic Grades</a>
      <a href="#" onclick="document.getElementById('requests-sec').scrollIntoView({behavior:'smooth'});return false;"><span class="ic">📄</span> Document Requests</a>

      <div class="nav-section-title" style="margin-top:20px;">SYSTEM NAVIGATION</div>
      <a href="admin_dashboard.php?switch_role=registrar"><span class="ic">↩️</span> Return to Registrar Portal</a>
    </nav>

    <div class="dash-sidebar-footer">
      <div class="user-chip">
        <div class="avatar-circle">👤</div>
        <div class="user-info">
          <div class="user-name"><?php echo htmlspecialchars($student['name']); ?></div>
          <div class="user-role"><?php echo htmlspecialchars($student['student_id_number']); ?></div>
        </div>
      </div>
    </div>
  </aside>

  <div class="dash-main">
    <header class="dash-topbar">
      <div class="topbar-left">
        <span class="term-pill">📅 AY <?php echo htmlspecialchars($currentSchoolYear); ?> &middot; <?php echo htmlspecialchars($currentSemester); ?></span>
        <span class="system-status-indicator"><span class="pulse-dot"></span> Student Portal Online</span>
      </div>
      <div class="dash-right">
        <a href="admin_dashboard.php?switch_role=registrar" class="topbar-btn">Switch to Registrar View</a>
        <span class="user-badge"><?php echo htmlspecialchars($student['name']); ?></span>
      </div>
    </header>

    <div class="dash-content">
      <?php if ($flash): ?>
        <div class="alert-banner alert-<?php echo htmlspecialchars($flash['type']); ?>">
          <span><?php echo htmlspecialchars($flash['message']); ?></span>
        </div>
      <?php endif; ?>

      <!-- Student Banner -->
      <div class="dash-panel" style="margin-bottom:20px;background:linear-gradient(135deg, #1e293b, #0f172a);color:#ffffff;border:none;">
        <div style="display:flex;justify-content:space-between;align-items:center;flex-wrap:wrap;gap:16px;">
          <div>
            <div style="font-size:12px;color:#94a3b8;text-transform:uppercase;letter-spacing:0.5px;">Welcome to Student Portal</div>
            <h1 style="margin:4px 0 6px;font-size:24px;color:#ffffff;"><?php echo htmlspecialchars($student['name']); ?></h1>
            <div style="font-size:13px;color:#cbd5e1;display:flex;gap:12px;flex-wrap:wrap;">
              <span><strong>ID:</strong> <?php echo htmlspecialchars($student['student_id_number']); ?></span>
              <span>&middot;</span>
              <span><?php echo htmlspecialchars($student['program']); ?> (<?php echo htmlspecialchars($student['year_level']); ?>)</span>
              <span>&middot;</span>
              <span><?php echo htmlspecialchars($student['email']); ?></span>
            </div>
          </div>
          <div>
            <button onclick="document.getElementById('docModal').style.display='block'" class="btn btn-primary">📄 Request Official Document</button>
          </div>
        </div>
      </div>

      <!-- KPI Metrics -->
      <div class="dash-stats">
        <div class="dash-stat-card">
          <div class="dash-stat-top"><div class="dash-stat-label">Cumulative GWA</div><div class="dash-stat-icon">📈</div></div>
          <div class="dash-stat-value" style="color:var(--accent);"><?php echo $student['average_grade'] ? number_format((float)$student['average_grade'], 2) : '1.25'; ?></div>
          <div class="dash-stat-sub">Official registrar recorded average</div>
        </div>

        <div class="dash-stat-card">
          <div class="dash-stat-top"><div class="dash-stat-label">Units Completed</div><div class="dash-stat-icon">📚</div></div>
          <div class="dash-stat-value"><?php echo (int)$student['completed_units']; ?> / 144</div>
          <div class="dash-stat-sub">Curriculum units earned</div>
        </div>

        <div class="dash-stat-card">
          <div class="dash-stat-top"><div class="dash-stat-label">Academic Standing</div><div class="dash-stat-icon">✔️</div></div>
          <div class="dash-stat-value" style="font-size:20px;"><span class="badge badge-<?php echo strtolower(str_replace(' ', '_', $student['academic_status'])); ?>"><?php echo htmlspecialchars($student['academic_status']); ?></span></div>
          <div class="dash-stat-sub">Registrar clearance status</div>
        </div>

        <div class="dash-stat-card">
          <div class="dash-stat-top"><div class="dash-stat-label">Guidance Standing</div><div class="dash-stat-icon">🤝</div></div>
          <div class="dash-stat-value" style="font-size:20px;"><span class="badge badge-<?php echo strtolower($student['guidance_clearance_status']); ?>"><?php echo htmlspecialchars($student['guidance_clearance_status']); ?></span></div>
          <div class="dash-stat-sub">Counseling clearance tag</div>
        </div>
      </div>

      <!-- Current Class Schedule -->
      <div class="dash-panel" id="schedule-sec">
        <div class="dash-panel-header">
          <h2>📅 Current Class Schedule (AY <?php echo htmlspecialchars($currentSchoolYear); ?> <?php echo htmlspecialchars($currentSemester); ?>)</h2>
        </div>
        <?php if (empty($schedule)): ?>
          <p style="color:#64748b;font-size:13px;text-align:center;padding:20px;">No enrolled classes found for the active semester.</p>
        <?php else: ?>
          <div class="table-responsive">
            <table class="table-simple">
              <thead>
                <tr>
                  <th>Section</th>
                  <th>Course Code</th>
                  <th>Course Title</th>
                  <th>Units</th>
                  <th>Room</th>
                  <th>Days &amp; Time</th>
                  <th>Instructor</th>
                </tr>
              </thead>
              <tbody>
                <?php foreach ($schedule as $sc): ?>
                <tr>
                  <td><strong style="color:var(--accent);"><?php echo htmlspecialchars($sc['section_code']); ?></strong></td>
                  <td><strong><?php echo htmlspecialchars($sc['subject_code']); ?></strong></td>
                  <td><?php echo htmlspecialchars($sc['subject_name']); ?></td>
                  <td><?php echo htmlspecialchars($sc['units']); ?>.0</td>
                  <td><span class="token-chip"><?php echo htmlspecialchars($sc['room']); ?></span></td>
                  <td>
                    <strong><?php echo htmlspecialchars($sc['days_of_week']); ?></strong>
                    <div style="font-size:11px;color:#64748b;"><?php echo date('h:i A', strtotime($sc['start_time'])); ?> – <?php echo date('h:i A', strtotime($sc['end_time'])); ?></div>
                  </td>
                  <td><?php echo htmlspecialchars($sc['instructor_name']); ?></td>
                </tr>
                <?php endforeach; ?>
              </tbody>
            </table>
          </div>
        <?php endif; ?>
      </div>

      <!-- Grades Ledger -->
      <div class="dash-panel" id="grades-sec">
        <div class="dash-panel-header">
          <h2>📜 Scholastic Grades &amp; Academic History</h2>
        </div>
        <?php if (empty($gradesList)): ?>
          <p style="color:#64748b;font-size:13px;text-align:center;padding:20px;">No grades recorded yet.</p>
        <?php else: ?>
          <div class="table-responsive">
            <table class="table-simple">
              <thead>
                <tr>
                  <th>Academic Term</th>
                  <th>Course Code</th>
                  <th>Descriptive Title</th>
                  <th>Units</th>
                  <th>Grade</th>
                  <th>Status</th>
                  <th>Remarks</th>
                </tr>
              </thead>
              <tbody>
                <?php foreach ($gradesList as $gl): 
                  $grVal = $gl['is_inc'] ? 'INC' : ($gl['grade'] !== null ? number_format((float)$gl['grade'], 2) : 'IP');
                  $isPassed = ($gl['grade'] !== null && (float)$gl['grade'] <= 3.00);
                ?>
                <tr>
                  <td><?php echo htmlspecialchars($gl['school_year'] . ' ' . $gl['semester']); ?></td>
                  <td><strong><?php echo htmlspecialchars($gl['subject_code']); ?></strong></td>
                  <td><?php echo htmlspecialchars($gl['subject_name']); ?></td>
                  <td><?php echo htmlspecialchars($gl['units']); ?>.0</td>
                  <td><strong style="font-size:14px;color:<?php echo $isPassed ? 'var(--success)' : ($gl['is_inc'] ? 'var(--purple)' : 'var(--text-main)'); ?>;"><?php echo $grVal; ?></strong></td>
                  <td><span class="badge badge-<?php echo strtolower($gl['grade_status'] ?: 'draft'); ?>"><?php echo htmlspecialchars(ucfirst($gl['grade_status'] ?: 'In Progress')); ?></span></td>
                  <td><?php echo htmlspecialchars($gl['remarks'] ?: '—'); ?></td>
                </tr>
                <?php endforeach; ?>
              </tbody>
            </table>
          </div>
        <?php endif; ?>
      </div>

      <!-- My Document Requests -->
      <div class="dash-panel" id="requests-sec">
        <div class="dash-panel-header">
          <h2>📄 My Official Document Requests</h2>
        </div>
        <?php if (empty($myRequests)): ?>
          <p style="color:#64748b;font-size:13px;text-align:center;padding:20px;">You have no active document requests.</p>
        <?php else: ?>
          <div class="table-responsive">
            <table class="table-simple">
              <thead>
                <tr>
                  <th>Tracking Token</th>
                  <th>Document Type</th>
                  <th>Purpose</th>
                  <th>Copies</th>
                  <th>Status</th>
                  <th>Requested Date</th>
                  <th>Registrar Note</th>
                </tr>
              </thead>
              <tbody>
                <?php foreach ($myRequests as $mr): ?>
                <tr>
                  <td><span class="token-chip"><?php echo htmlspecialchars($mr['qr_code_token']); ?></span></td>
                  <td><strong><?php echo htmlspecialchars($mr['document_type']); ?></strong></td>
                  <td><?php echo htmlspecialchars($mr['purpose']); ?></td>
                  <td><?php echo $mr['copies']; ?></td>
                  <td><span class="badge badge-<?php echo strtolower($mr['status']); ?>"><?php echo htmlspecialchars(ucfirst($mr['status'])); ?></span></td>
                  <td><?php echo date('M d, Y', strtotime($mr['requested_at'])); ?></td>
                  <td style="font-size:12px;color:#475569;"><?php echo htmlspecialchars($mr['remarks'] ?: 'Processing at Registrar window'); ?></td>
                </tr>
                <?php endforeach; ?>
              </tbody>
            </table>
          </div>
        <?php endif; ?>
      </div>

    </div>
  </div>

  <!-- Modal: Request Document -->
  <div id="docModal" style="display:none;position:fixed;top:0;left:0;right:0;bottom:0;background:rgba(15,23,42,0.6);z-index:999;">
    <div style="max-width:500px;background:#fff;border-radius:12px;margin:80px auto;padding:24px;box-shadow:0 20px 25px -5px rgba(0,0,0,0.2);">
      <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px;">
        <h2 style="margin:0;font-size:18px;">Request Official Document</h2>
        <button onclick="document.getElementById('docModal').style.display='none'" style="background:none;border:none;font-size:20px;cursor:pointer;">&times;</button>
      </div>
      <form method="post">
        <input type="hidden" name="action" value="request_document">

        <div class="form-group" style="margin-bottom:12px;">
          <label>Select Document Type *</label>
          <select name="document_type" required>
            <option value="Official Transcript of Records (TOR)">Official Transcript of Records (TOR)</option>
            <option value="Certificate of Good Moral Character">Certificate of Good Moral Character</option>
            <option value="Certificate of Grades">Certificate of Grades</option>
            <option value="Certificate of Enrollment">Certificate of Enrollment</option>
            <option value="Certified True Copy (CTC)">Certified True Copy (CTC)</option>
          </select>
        </div>

        <div class="form-group" style="margin-bottom:12px;">
          <label>Number of Copies</label>
          <input type="number" name="copies" value="1" min="1" max="5">
        </div>

        <div class="form-group" style="margin-bottom:20px;">
          <label>Purpose of Request *</label>
          <input type="text" name="purpose" placeholder="e.g. Scholarship application, Employment, PRC Exam" required>
        </div>

        <div style="display:flex;justify-content:flex-end;gap:10px;">
          <button type="button" class="btn btn-secondary" onclick="document.getElementById('docModal').style.display='none'">Cancel</button>
          <button type="submit" class="btn btn-primary">Submit Request</button>
        </div>
      </form>
    </div>
  </div>

</body>
</html>