<?php
require 'config.php';
$displayName = $_SESSION['user_name'] ?? 'Registrar';
?>
<!doctype html>
<html>
<head>
<meta charset="utf-8">
<title>Curriculum Evaluation & Crediting - Registrar</title>
<link rel="stylesheet" href="styles.css">
<link rel="stylesheet" href="dashboard.css">
</head>
<body class="dashboard-body">

  <aside class="dash-sidebar">
    <div class="dash-brand"><span class="cap">🎓</span> Registrar System</div>
    <nav class="dash-nav">
      <a href="admin_dashboard.php"><span class="ic">🏠</span> Dashboard</a>
      <a href="student_records.php"><span class="ic">🗂️</span> Student Records (201 File)</a>
      <a href="enrollment_validation.php"><span class="ic">📝</span> Enrollment &amp; Validation</a>
      <a href="class_scheduling.php"><span class="ic">🏫</span> Class Scheduling</a>
      <a href="grade_control.php"><span class="ic">📊</span> Grade Control</a>
      <a href="curriculum_evaluation.php" class="active"><span class="ic">🧭</span> Curriculum Evaluation</a>
      <a href="graduation_audit.php"><span class="ic">🎓</span> Graduation &amp; Honors</a>
      <a href="document_processing.php"><span class="ic">📄</span> Document Processing</a>
      <a href="government_compliance.php"><span class="ic">🏛️</span> Gov't Compliance</a>
    </nav>
  </aside>

  <div class="dash-main">
    <div class="dash-topbar">
      <div class="dash-title">🎓 Registrar System</div>
      <div class="dash-right">
        <span class="bell">🔔</span>
        <span><span class="avatar">👤</span><?php echo htmlspecialchars($displayName); ?></span>
      </div>
    </div>

    <div class="dash-content">
      <h1 class="dash-heading">🧭 Curriculum Evaluation &amp; Crediting</h1>
      <p class="dash-subheading">Evaluate transferee credits, process course shifts, and audit curriculum progress.</p>

      <div class="dash-panel">
        <div class="placeholder-note">🚧 This module isn't built yet — functionality coming soon.</div>
        <h2>Planned Features</h2>
        <ul class="feature-list">
          <li><strong>Transferee Subject Crediting</strong> — Evaluate and set which subjects from a transferee's previous school can be credited.</li>
          <li><strong>Course Shifting Approval</strong> — Process a student's course change and migrate their earned credits to the new curriculum.</li>
          <li><strong>Curriculum Progress Audit</strong> — Check whether a student is following their course flowchart/curriculum.</li>
        </ul>
      </div>
    </div>
  </div>

</body>
</html>