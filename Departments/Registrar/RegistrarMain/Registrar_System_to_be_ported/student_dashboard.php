<?php
require 'config.php';
require_role('student');

$userId = (int) $_SESSION['user_id'];

$stmt = $pdo->prepare(
    "SELECT program, year_level, average_grade, academic_status, completed_units, units_remaining
     FROM student_profile WHERE user_id = ?"
);
$stmt->execute([$userId]);
$profile = $stmt->fetch();

// A student account with no profile row yet still gets a working page.
$profile = $profile ?: [
    'program'          => 'Not set',
    'year_level'       => 'Not set',
    'average_grade'    => null,
    'academic_status'  => 'Good Standing',
    'completed_units'  => 0,
    'units_remaining'  => 0,
];

$currentSchoolYear = $pdo->query("SELECT `value` FROM settings WHERE `key` = 'current_school_year'")->fetchColumn() ?: '';
$currentSemester   = $pdo->query("SELECT `value` FROM settings WHERE `key` = 'current_semester'")->fetchColumn() ?: '';

$stmt = $pdo->prepare(
    "SELECT message, created_at FROM activity_log WHERE user_id = ? ORDER BY created_at DESC LIMIT 4"
);
$stmt->execute([$userId]);
$recentActivity = $stmt->fetchAll();

$displayName = $_SESSION['user_name'] ?? 'Student';
$avgGradeDisplay = $profile['average_grade'] !== null ? number_format((float) $profile['average_grade'], 1) . '%' : 'N/A';
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
      <a href="student_dashboard.php" class="active"><span class="ic">🏠</span> Dashboard</a>
      <a href="#"><span class="ic">🪪</span> My Profile</a>
      <a href="#"><span class="ic">📘</span> Academic Portal</a>
      <a href="#"><span class="ic">🎓</span> Student</a>
      <a href="#"><span class="ic">📈</span> Grades &amp; Performance</a>
      <a href="#"><span class="ic">📄</span> Document Requests</a>
      <a href="#"><span class="ic">⚙️</span> Settings</a>
    </nav>
    <a href="logout.php" class="dash-logout"><span class="ic">↩️</span> Logout</a>
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
      <p class="dash-subheading">Welcome back, <?php echo htmlspecialchars($displayName); ?>. Here's an overview of your academic records.</p>

      <div class="dash-stats">
        <div class="dash-stat-card">
          <div class="dash-stat-top">
            <div class="dash-stat-icon">🎓</div>
            <div class="dash-stat-label">Current Program</div>
          </div>
          <div class="dash-stat-value"><?php echo htmlspecialchars($profile['program']); ?></div>
        </div>
        <div class="dash-stat-card">
          <div class="dash-stat-top">
            <div class="dash-stat-icon">📚</div>
            <div class="dash-stat-label">Year Level</div>
          </div>
          <div class="dash-stat-value"><?php echo htmlspecialchars($profile['year_level']); ?></div>
        </div>
        <div class="dash-stat-card">
          <div class="dash-stat-top">
            <div class="dash-stat-icon">📈</div>
            <div class="dash-stat-label">Average Grade</div>
          </div>
          <div class="dash-stat-value"><?php echo htmlspecialchars($avgGradeDisplay); ?></div>
        </div>
        <div class="dash-stat-card">
          <div class="dash-stat-top">
            <div class="dash-stat-icon">✔️</div>
            <div class="dash-stat-label">Academic Status</div>
          </div>
          <div class="dash-stat-value dash-stat-value-sm"><?php echo htmlspecialchars($profile['academic_status']); ?></div>
        </div>
      </div>

      <div class="dash-grid">
        <div class="dash-panel">
          <h2>Recent Activity</h2>
          <?php if (!$recentActivity): ?>
            <p class="empty-note">No activity yet.</p>
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
          <a href="#" class="quick-action"><span class="qa-left">📘 View My Records</span> ›</a>
          <a href="#" class="quick-action"><span class="qa-left">📈 View My Grades</span> ›</a>
          <a href="#" class="quick-action"><span class="qa-left">📄 Request Document</span> ›</a>
          <a href="#" class="quick-action"><span class="qa-left">🔎 View Transcript</span> ›</a>
        </div>
      </div>

      <div class="dash-bottom-grid">
        <div class="dash-panel">
          <h2>Academic Snapshot</h2>
          <div class="status-row">
            <span>Completed Units</span>
            <span><?php echo number_format((int) $profile['completed_units']); ?></span>
          </div>
          <div class="status-row">
            <span>Units Remaining</span>
            <span><?php echo number_format((int) $profile['units_remaining']); ?></span>
          </div>
        </div>

        <div class="dash-panel">
          <h2>Current Semester</h2>
          <div class="status-row">
            <span>Semester</span>
            <span><?php echo htmlspecialchars($currentSemester); ?></span>
          </div>
          <div class="status-row">
            <span>School Year</span>
            <span><?php echo htmlspecialchars($currentSchoolYear); ?></span>
          </div>
        </div>
      </div>
    </div>
  </div>

</body>
</html>