<?php
require 'config.php';

/**
 * Everything below is a live query against registrar_db — no hardcoded
 * sample data. See registrar_db.sql for the schema these read from.
 */

$currentSchoolYear = $pdo->query("SELECT `value` FROM settings WHERE `key` = 'current_school_year'")->fetchColumn() ?: '';
$currentSemester   = $pdo->query("SELECT `value` FROM settings WHERE `key` = 'current_semester'")->fetchColumn() ?: '';

$totalStudents = (int) $pdo->query("SELECT COUNT(*) FROM `user` WHERE role = 'student'")->fetchColumn();

$stmt = $pdo->prepare(
    "SELECT COUNT(*) FROM enrollments
     WHERE enrollment_type = 'new' AND school_year = ? AND status = 'active'"
);
$stmt->execute([$currentSchoolYear]);
$newEnrollments = (int) $stmt->fetchColumn();

$graduatingStudents = (int) $pdo->query(
    "SELECT COUNT(*) FROM student_profile WHERE academic_status = 'Graduating'"
)->fetchColumn();

$pendingRequests = (int) $pdo->query(
    "SELECT COUNT(*) FROM transcript_requests WHERE status = 'pending'"
)->fetchColumn();

$recentActivity = $pdo->query(
    "SELECT message, created_at FROM activity_log ORDER BY created_at DESC LIMIT 4"
)->fetchAll();

// Enrollment by year, always shows all four years even if a year has 0 students
$yearOrder = ['1st Year', '2nd Year', '3rd Year', '4th Year'];
$rows = $pdo->query(
    "SELECT sp.year_level, COUNT(*) AS cnt
     FROM student_profile sp
     JOIN `user` u ON u.id = sp.user_id AND u.role = 'student'
     GROUP BY sp.year_level"
)->fetchAll(PDO::FETCH_KEY_PAIR);
$enrollmentByYear = [];
foreach ($yearOrder as $y) {
    $enrollmentByYear[$y] = (int) ($rows[$y] ?? 0);
}

$systemStatus = $pdo->query("SELECT service_name, status FROM system_status")->fetchAll();
$systemStatus[] = [
    'service_name' => 'Requests Queue',
    'status'       => $pendingRequests . ' Pending',
];

$displayName = $_SESSION['user_name'] ?? 'Admin';
?>
<!doctype html>
<html>
<head>
<meta charset="utf-8">
<title>Dashboard - Registrar</title>
<link rel="stylesheet" href="styles.css">
<link rel="stylesheet" href="dashboard.css">
</head>
<body class="dashboard-body">

  <aside class="dash-sidebar">
    <div class="dash-brand"><span class="cap">🎓</span> Registrar System</div>
    <nav class="dash-nav">
      <a href="admin_dashboard.php" class="active"><span class="ic">🏠</span> Dashboard</a>
      <a href="student_records.php"><span class="ic">🗂️</span> Student Records (201 File)</a>
      <a href="enrollment_validation.php"><span class="ic">📝</span> Enrollment &amp; Validation</a>
      <a href="class_scheduling.php"><span class="ic">🏫</span> Class Scheduling</a>
      <a href="grade_control.php"><span class="ic">📊</span> Grade Control</a>
      <a href="curriculum_evaluation.php"><span class="ic">🧭</span> Curriculum Evaluation</a>
      <a href="graduation_audit.php"><span class="ic">🎓</span> Graduation &amp; Honors</a>
      <a href="document_processing.php"><span class="ic">📄</span> Document Processing</a>
      <a href="government_compliance.php"><span class="ic">🏛️</span> Gov't Compliance</a>
    </nav>
  </aside>

  <div class="dash-main">
    <div class="dash-topbar">
      <div class="dash-title"></div>
      <div class="dash-right">
        <span class="bell">🔔</span>
        <span><span class="avatar">👤</span><?php echo htmlspecialchars($displayName); ?></span>
      </div>
    </div>

    <div class="dash-content">
      <h1 class="dash-heading">🏠 Dashboard</h1>
      <p class="dash-subheading">Welcome back, <?php echo htmlspecialchars($displayName); ?>. Here's an overview of the registrar system.</p>

      <div class="dash-stats">
        <div class="dash-stat-card">
          <div class="dash-stat-top">
            <div class="dash-stat-icon">👥</div>
            <div class="dash-stat-label">Total Students</div>
          </div>
          <div class="dash-stat-value"><?php echo number_format($totalStudents); ?></div>
        </div>
        <div class="dash-stat-card">
          <div class="dash-stat-top">
            <div class="dash-stat-icon">📝</div>
            <div class="dash-stat-label">New Enrollments</div>
          </div>
          <div class="dash-stat-value"><?php echo number_format($newEnrollments); ?></div>
        </div>
        <div class="dash-stat-card">
          <div class="dash-stat-top">
            <div class="dash-stat-icon">🎓</div>
            <div class="dash-stat-label">Graduating Students</div>
          </div>
          <div class="dash-stat-value"><?php echo number_format($graduatingStudents); ?></div>
        </div>
        <div class="dash-stat-card">
          <div class="dash-stat-top">
            <div class="dash-stat-icon">📄</div>
            <div class="dash-stat-label">Pending Requests</div>
          </div>
          <div class="dash-stat-value"><?php echo number_format($pendingRequests); ?></div>
        </div>
      </div>

      <div class="dash-grid">
        <div class="dash-panel">
          <h2>Recent Activity</h2>
          <?php if (!$recentActivity): ?>
            <p class="empty-note">No activity logged yet.</p>
          <?php endif; ?>
          <?php foreach ($recentActivity as $item): ?>
            <div class="activity-row">
              <span class="activity-time"><?php echo htmlspecialchars(date('h:i A', strtotime($item['created_at']))); ?></span>
              <span><?php echo htmlspecialchars($item['message']); ?></span>
            </div>
          <?php endforeach; ?>
        </div>

        <div class="dash-panel">
          <h2>Quick Actions</h2>
          <a href="student_records.php" class="quick-action"><span class="qa-left">➕ Add / Manage Student</span> ›</a>
          <a href="student_records.php" class="quick-action"><span class="qa-left">🔍 Search Student</span> ›</a>
          <a href="document_processing.php" class="quick-action"><span class="qa-left">📝 Generate Document</span> ›</a>
          <a href="#" class="quick-action"><span class="qa-left">📊 View Analytics</span> ›</a>
        </div>
      </div>

      <div class="dash-bottom-grid">
        <div class="dash-panel">
          <h2>Enrollment Summary</h2>
          <div class="enroll-cols">
            <?php foreach ($enrollmentByYear as $year => $count): ?>
              <div class="enroll-col">
                <div class="yr-label"><?php echo htmlspecialchars($year); ?></div>
                <div class="yr-value"><?php echo number_format($count); ?></div>
              </div>
            <?php endforeach; ?>
          </div>
        </div>

        <div class="dash-panel">
          <h2>System Status</h2>
          <?php foreach ($systemStatus as $row):
              $isPending = str_ends_with($row['status'], 'Pending');
              $class = $isPending ? 'status-pending' : 'status-ok';
          ?>
            <div class="status-row">
              <span><?php echo htmlspecialchars($row['service_name']); ?></span>
              <span class="<?php echo $class; ?>"><?php echo htmlspecialchars($row['status']); ?></span>
            </div>
          <?php endforeach; ?>
        </div>
      </div>
    </div>
  </div>

</body>
</html>