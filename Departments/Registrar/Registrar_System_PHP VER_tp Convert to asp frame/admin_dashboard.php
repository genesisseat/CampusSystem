<?php
require 'config.php';

// If arriving via switch_role, config.php already swapped the session — redirect clean
if (isset($_GET['switch_role'])) {
    header('Location: admin_dashboard.php');
    exit;
}

// Fetch Term settings
$currentSchoolYear = get_setting($pdo, 'current_school_year', '2025-2026');
$currentSemester   = get_setting($pdo, 'current_semester', '1st Semester');
$encodingOpen      = get_setting($pdo, 'grade_encoding_open', '1');
$encodingDeadline  = get_setting($pdo, 'grade_encoding_deadline', '2025-10-30');

// Handle Quick Settings Form POST
if ($_SERVER['REQUEST_METHOD'] === 'POST' && isset($_POST['action']) && $_POST['action'] === 'save_term_settings') {
    $sy = trim($_POST['school_year'] ?? '');
    $sem = trim($_POST['semester'] ?? '');
    $enc = isset($_POST['encoding_open']) ? '1' : '0';
    $dead = trim($_POST['deadline'] ?? '');

    if ($sy && $sem) {
        set_setting($pdo, 'current_school_year', $sy);
        set_setting($pdo, 'current_semester', $sem);
        set_setting($pdo, 'grade_encoding_open', $enc);
        set_setting($pdo, 'grade_encoding_deadline', $dead);
        log_activity($pdo, $_SESSION['user_id'] ?? null, "Academic term updated to AY {$sy} {$sem}. Grade encoding window: " . ($enc === '1' ? 'OPEN' : 'CLOSED'));
        set_flash("Academic Term settings updated successfully.");
    }
    header('Location: admin_dashboard.php');
    exit;
}

// Live Statistics
$totalStudents = (int) $pdo->query("SELECT COUNT(*) FROM `user` WHERE role = 'student'")->fetchColumn();

$stmt = $pdo->prepare("SELECT COUNT(*) FROM enrollments WHERE school_year = ? AND semester = ? AND status = 'active'");
$stmt->execute([$currentSchoolYear, $currentSemester]);
$activeEnrollments = (int) $stmt->fetchColumn();

$graduatingStudents = (int) $pdo->query("SELECT COUNT(*) FROM student_profile WHERE academic_status = 'Graduating'")->fetchColumn();

// Pending counts across queues
$pendingDocs = (int) $pdo->query("SELECT COUNT(*) FROM transcript_requests WHERE status IN ('pending', 'processing')")->fetchColumn();
$pendingAddDrop = (int) $pdo->query("SELECT COUNT(*) FROM add_drop_requests WHERE status = 'pending'")->fetchColumn();
$pendingOverload = (int) $pdo->query("SELECT COUNT(*) FROM overload_waiver_requests WHERE status = 'pending'")->fetchColumn();
$pendingRevisions = (int) $pdo->query("SELECT COUNT(*) FROM completion_revision_requests WHERE status = 'pending'")->fetchColumn();
$totalPendingQueue = $pendingDocs + $pendingAddDrop + $pendingOverload + $pendingRevisions;

$activeOfferingsCount = (int) $pdo->query("SELECT COUNT(*) FROM class_offerings WHERE status = 'open'")->fetchColumn();

// Recent Activity Log
$recentActivity = $pdo->query("SELECT message, created_at FROM activity_log ORDER BY created_at DESC LIMIT 6")->fetchAll();

// Distribution by Program
$programDistribution = $pdo->query("SELECT program, COUNT(*) as cnt FROM student_profile GROUP BY program ORDER BY cnt DESC")->fetchAll();

// Distribution by Year
$yearOrder = ['1st Year', '2nd Year', '3rd Year', '4th Year'];
$rows = $pdo->query("SELECT sp.year_level, COUNT(*) AS cnt FROM student_profile sp JOIN `user` u ON u.id = sp.user_id AND u.role = 'student' GROUP BY sp.year_level")->fetchAll(PDO::FETCH_KEY_PAIR);
$enrollmentByYear = [];
foreach ($yearOrder as $y) {
    $enrollmentByYear[$y] = (int) ($rows[$y] ?? 0);
}

// System Status
$systemStatus = $pdo->query("SELECT service_name, status FROM system_status")->fetchAll();
$flash = get_flash();
?>
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Registrar Executive Dashboard - MSU</title>
  <link rel="stylesheet" href="dashboard.css">
</head>
<body class="dashboard-body">

  <?php render_sidebar('admin_dashboard.php'); ?>

  <div class="dash-main">
    <?php render_topbar($pdo, 'Executive Dashboard'); ?>

    <div class="dash-content">
      <?php if ($flash): ?>
        <div class="alert-banner alert-<?php echo htmlspecialchars($flash['type']); ?>">
          <span><?php echo htmlspecialchars($flash['message']); ?></span>
        </div>
      <?php endif; ?>

      <div class="dash-heading-row">
        <div>
          <h1 class="dash-heading">Executive Registrar Dashboard</h1>
          <p class="dash-subheading">Central overview for academic records, active term enrollments, and institution compliance.</p>
        </div>
        <div>
          <button class="btn btn-secondary" onclick="document.getElementById('termModal').style.display='block'">⚙️ Term Settings</button>
        </div>
      </div>

      <!-- Quick KPI Stats -->
      <div class="dash-stats">
        <div class="dash-stat-card">
          <div class="dash-stat-top">
            <div class="dash-stat-label">Active Enrollments</div>
            <div class="dash-stat-icon">📝</div>
          </div>
          <div class="dash-stat-value"><?php echo number_format($activeEnrollments); ?></div>
          <div class="dash-stat-sub">Validated for AY <?php echo htmlspecialchars($currentSchoolYear); ?></div>
        </div>

        <div class="dash-stat-card">
          <div class="dash-stat-top">
            <div class="dash-stat-label">Graduating Candidates</div>
            <div class="dash-stat-icon">🎓</div>
          </div>
          <div class="dash-stat-value"><?php echo number_format($graduatingStudents); ?></div>
          <div class="dash-stat-sub">Ready for honors &amp; S.O. audit</div>
        </div>

        <div class="dash-stat-card">
          <div class="dash-stat-top">
            <div class="dash-stat-label">Pending Queues</div>
            <div class="dash-stat-icon">⏳</div>
          </div>
          <div class="dash-stat-value"><?php echo number_format($totalPendingQueue); ?></div>
          <div class="dash-stat-sub"><?php echo $pendingDocs; ?> docs &middot; <?php echo $pendingAddDrop + $pendingOverload; ?> validation petitions</div>
        </div>

        <div class="dash-stat-card">
          <div class="dash-stat-top">
            <div class="dash-stat-label">Open Class Offerings</div>
            <div class="dash-stat-icon">🏫</div>
          </div>
          <div class="dash-stat-value"><?php echo number_format($activeOfferingsCount); ?></div>
          <div class="dash-stat-sub">Scheduled class sections</div>
        </div>
      </div>

      <!-- Main Two Column Grid -->
      <div class="dash-grid">
        <div class="dash-panel">
          <div class="dash-panel-header">
            <h2>📊 Student Distribution by Year Level &amp; Program</h2>
          </div>
          <div class="stat-mini-row" style="margin-bottom:20px;">
            <?php foreach ($enrollmentByYear as $yr => $cnt): ?>
              <div class="stat-mini">
                <div class="label"><?php echo htmlspecialchars($yr); ?></div>
                <div class="value"><?php echo number_format($cnt); ?> <span style="font-size:11px;font-weight:normal;color:#64748b;">students</span></div>
              </div>
            <?php endforeach; ?>
          </div>

          <h3 style="font-size:13px;text-transform:uppercase;color:#64748b;margin-bottom:10px;">Program Enrollment Breakdown</h3>
          <table class="table-simple">
            <thead>
              <tr>
                <th>Academic Degree Program</th>
                <th style="text-align:right;">Enrolled Students</th>
                <th style="text-align:right;">Share %</th>
              </tr>
            </thead>
            <tbody>
              <?php foreach ($programDistribution as $prog): 
                $pct = $totalStudents > 0 ? round(($prog['cnt'] / $totalStudents) * 100, 1) : 0;
              ?>
              <tr>
                <td><strong><?php echo htmlspecialchars($prog['program']); ?></strong></td>
                <td style="text-align:right;"><?php echo number_format($prog['cnt']); ?></td>
                <td style="text-align:right;color:#64748b;"><?php echo $pct; ?>%</td>
              </tr>
              <?php endforeach; ?>
            </tbody>
          </table>
        </div>

        <div>
          <!-- Quick Actions -->
          <div class="dash-panel">
            <div class="dash-panel-header">
              <h2>⚡ Quick Access</h2>
            </div>
            <a href="student_records.php" class="quick-action">
              <span>🗂️ Search / Manage 201 Files</span>
              <span>›</span>
            </a>
            <a href="enrollment_validation.php" class="quick-action">
              <span>📝 Validate Pending Enrollments</span>
              <span>›</span>
            </a>
            <a href="document_processing.php" class="quick-action">
              <span>📄 Issue TOR / Good Moral</span>
              <span>›</span>
            </a>
            <a href="grade_control.php" class="quick-action">
              <span>📊 Grade Verification &amp; Locking</span>
              <span>›</span>
            </a>
            <a href="graduation_audit.php" class="quick-action">
              <span>🎓 Latin Honors &amp; Degree Audit</span>
              <span>›</span>
            </a>
            <a href="integration_hub.php" class="quick-action">
              <span>🔌 Connect Student / Faculty / Guidance</span>
              <span>›</span>
            </a>
          </div>

          <!-- System & Interop Status -->
          <div class="dash-panel">
            <div class="dash-panel-header">
              <h2>📡 System Heartbeat &amp; Interop</h2>
            </div>
            <?php foreach ($systemStatus as $row): 
              $isOk = in_array(strtolower($row['status']), ['operational', 'online', 'sync active', 'open', 'ready', 'connected']);
            ?>
              <div style="display:flex;justify-content:space-between;padding:8px 0;border-bottom:1px solid #e2e8f0;font-size:13px;">
                <span><?php echo htmlspecialchars($row['service_name']); ?></span>
                <span class="badge <?php echo $isOk ? 'badge-active' : 'badge-pending'; ?>"><?php echo htmlspecialchars($row['status']); ?></span>
              </div>
            <?php endforeach; ?>
          </div>
        </div>
      </div>

      <!-- Recent Audit Log -->
      <div class="dash-panel">
        <div class="dash-panel-header">
          <h2>🕒 Recent Registrar Audit Trail</h2>
        </div>
        <?php if (!$recentActivity): ?>
          <p style="color:#94a3b8;font-size:13px;">No system activity recorded yet.</p>
        <?php else: ?>
          <?php foreach ($recentActivity as $act): ?>
            <div class="activity-row">
              <span class="activity-time"><?php echo htmlspecialchars(date('M d, h:i A', strtotime($act['created_at']))); ?></span>
              <span><?php echo htmlspecialchars($act['message']); ?></span>
            </div>
          <?php endforeach; ?>
        <?php endif; ?>
      </div>

    </div>
  </div>

  <!-- Term Settings Modal -->
  <div id="termModal" style="display:none;position:fixed;top:0;left:0;right:0;bottom:0;background:rgba(15,23,42,0.6);z-index:999;">
    <div style="max-width:480px;background:#fff;border-radius:12px;margin:80px auto;padding:24px;box-shadow:0 20px 25px -5px rgba(0,0,0,0.2);">
      <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px;">
        <h2 style="margin:0;font-size:18px;">Academic Term Settings</h2>
        <button onclick="document.getElementById('termModal').style.display='none'" style="background:none;border:none;font-size:20px;cursor:pointer;">&times;</button>
      </div>
      <form method="post">
        <input type="hidden" name="action" value="save_term_settings">
        <div class="form-group" style="margin-bottom:12px;">
          <label>Academic School Year</label>
          <input type="text" name="school_year" value="<?php echo htmlspecialchars($currentSchoolYear); ?>" required>
        </div>
        <div class="form-group" style="margin-bottom:12px;">
          <label>Academic Semester</label>
          <select name="semester">
            <option value="1st Semester" <?php echo $currentSemester === '1st Semester' ? 'selected' : ''; ?>>1st Semester</option>
            <option value="2nd Semester" <?php echo $currentSemester === '2nd Semester' ? 'selected' : ''; ?>>2nd Semester</option>
            <option value="Summer Term" <?php echo $currentSemester === 'Summer Term' ? 'selected' : ''; ?>>Summer Term</option>
          </select>
        </div>
        <div class="form-group" style="margin-bottom:12px;">
          <label style="display:flex;align-items:center;gap:8px;">
            <input type="checkbox" name="encoding_open" value="1" <?php echo $encodingOpen === '1' ? 'checked' : ''; ?>>
            Grade Encoding Window Open
          </label>
        </div>
        <div class="form-group" style="margin-bottom:18px;">
          <label>Encoding Deadline</label>
          <input type="date" name="deadline" value="<?php echo htmlspecialchars($encodingDeadline); ?>">
        </div>
        <div style="display:flex;justify-content:flex-end;gap:10px;">
          <button type="button" class="btn btn-secondary" onclick="document.getElementById('termModal').style.display='none'">Cancel</button>
          <button type="submit" class="btn btn-primary">Save Settings</button>
        </div>
      </form>
    </div>
  </div>

</body>
</html>