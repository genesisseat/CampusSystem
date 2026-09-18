<?php
require 'config.php';
$displayName = $_SESSION['user_name'] ?? 'Registrar';

// Pull pending transcript_requests as a preview, since the schema already
// tracks these — the request queue itself is not yet built.
$pendingRequests = $pdo->query(
    "SELECT tr.id, u.name, tr.document_type, tr.status, tr.requested_at
     FROM transcript_requests tr
     JOIN `user` u ON u.id = tr.student_id
     ORDER BY tr.requested_at DESC LIMIT 10"
)->fetchAll();
?>
<!doctype html>
<html>
<head>
<meta charset="utf-8">
<title>Document Processing & Issuance - Registrar</title>
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
      <a href="curriculum_evaluation.php"><span class="ic">🧭</span> Curriculum Evaluation</a>
      <a href="graduation_audit.php"><span class="ic">🎓</span> Graduation &amp; Honors</a>
      <a href="document_processing.php" class="active"><span class="ic">📄</span> Document Processing</a>
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
      <h1 class="dash-heading">📄 Official Document Processing &amp; Issuance</h1>
      <p class="dash-subheading">Manage requests for TOR, diploma, certifications, and secure document printing.</p>

      <div class="dash-panel" style="margin-bottom:18px;">
        <div class="placeholder-note">🚧 Full queue management and printing tools aren't built yet — showing a read-only preview from existing data.</div>
        <h2>Recent Document Requests</h2>
        <?php if (!$pendingRequests): ?>
          <p class="empty-note">No document requests yet.</p>
        <?php else: ?>
          <table class="table-simple">
            <thead><tr><th>Student</th><th>Document</th><th>Status</th><th>Requested</th></tr></thead>
            <tbody>
              <?php foreach ($pendingRequests as $r): ?>
              <tr>
                <td><?php echo htmlspecialchars($r['name']); ?></td>
                <td><?php echo htmlspecialchars($r['document_type']); ?></td>
                <td><span class="badge <?php echo $r['status'] === 'pending' ? 'badge-on-leave' : 'badge-verified'; ?>"><?php echo htmlspecialchars(ucfirst($r['status'])); ?></span></td>
                <td><?php echo htmlspecialchars(date('M d, Y', strtotime($r['requested_at']))); ?></td>
              </tr>
              <?php endforeach; ?>
            </tbody>
          </table>
        <?php endif; ?>
      </div>

      <div class="dash-panel">
        <h2>Planned Features</h2>
        <ul class="feature-list">
          <li><strong>Document Request Queue</strong> — Manage student/alumni requests for Transcript of Records (TOR), Diploma, Certificate of Transfer / Honorable Dismissal, Certificate of Grades / Units Earned / Enrolment, and Certified True Copies (CTC).</li>
          <li><strong>Document Printing &amp; QR Security</strong> — Generate and print official documents with an official seal, dry seal area, or security QR code.</li>
        </ul>
      </div>
    </div>
  </div>

</body>
</html>