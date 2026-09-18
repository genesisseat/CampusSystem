<?php
require 'config.php';

$displayName = $_SESSION['user_name'] ?? 'Registrar';

// Standard checklist of documents every student's 201 file must track.
const VAULT_DOCUMENT_TYPES = [
    'Form 137',
    'Form 138',
    'Birth Certificate',
    'Good Moral',
    'Transcript from Previous School',
];

const ENROLLMENT_STATUSES = ['Active', 'On-Leave', 'Dropped', 'Dismissed', 'Graduated'];

/** Make sure every document type in the checklist has a row for this student. */
function ensure_vault_rows(PDO $pdo, int $studentId): void
{
    $stmt = $pdo->prepare(
        'INSERT IGNORE INTO document_credentials (student_id, document_type) VALUES (?, ?)'
    );
    foreach (VAULT_DOCUMENT_TYPES as $type) {
        $stmt->execute([$studentId, $type]);
    }
}

function badge_class_enrollment(string $status): string
{
    return match ($status) {
        'Active'     => 'badge-active',
        'On-Leave'   => 'badge-on-leave',
        'Dropped'    => 'badge-dropped',
        'Dismissed'  => 'badge-dismissed',
        'Graduated'  => 'badge-graduated',
        default      => '',
    };
}

function badge_class_vault(string $status): string
{
    return match ($status) {
        'Missing'   => 'badge-missing',
        'Submitted' => 'badge-submitted',
        'Verified'  => 'badge-verified',
        default     => '',
    };
}

$flash = '';
$viewId = isset($_GET['id']) ? (int) $_GET['id'] : 0;

// ---- Handle form submissions (PRG pattern: redirect back after processing) ----
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $action = $_POST['action'] ?? '';
    $studentId = (int) ($_POST['student_id'] ?? 0);

    if ($action === 'update_enrollment_status' && $studentId > 0) {
        $newStatus = $_POST['enrollment_status'] ?? '';
        if (in_array($newStatus, ENROLLMENT_STATUSES, true)) {
            $stmt = $pdo->prepare('UPDATE student_profile SET enrollment_status = ? WHERE user_id = ?');
            $stmt->execute([$newStatus, $studentId]);

            $log = $pdo->prepare('INSERT INTO activity_log (user_id, message) VALUES (?, ?)');
            $log->execute([$studentId, "Academic status changed to \"{$newStatus}\" by {$displayName}"]);
        }
        header('Location: student_records.php?id=' . $studentId . '&saved=1');
        exit;
    }

    if ($action === 'update_document' && $studentId > 0) {
        $docId = (int) ($_POST['doc_id'] ?? 0);
        $newStatus = $_POST['doc_status'] ?? '';
        if ($docId > 0 && in_array($newStatus, ['Missing', 'Submitted', 'Verified'], true)) {
            if ($newStatus === 'Missing') {
                $stmt = $pdo->prepare(
                    'UPDATE document_credentials SET status = ?, submitted_at = NULL, verified_at = NULL
                     WHERE id = ? AND student_id = ?'
                );
                $stmt->execute([$newStatus, $docId, $studentId]);
            } elseif ($newStatus === 'Submitted') {
                $stmt = $pdo->prepare(
                    'UPDATE document_credentials
                     SET status = ?, submitted_at = COALESCE(submitted_at, ?), verified_at = NULL
                     WHERE id = ? AND student_id = ?'
                );
                $stmt->execute([$newStatus, date('Y-m-d H:i:s'), $docId, $studentId]);
            } else { // Verified
                $now = date('Y-m-d H:i:s');
                $stmt = $pdo->prepare(
                    'UPDATE document_credentials
                     SET status = ?, submitted_at = COALESCE(submitted_at, ?), verified_at = ?
                     WHERE id = ? AND student_id = ?'
                );
                $stmt->execute([$newStatus, $now, $now, $docId, $studentId]);
            }

            $log = $pdo->prepare('INSERT INTO activity_log (user_id, message) VALUES (?, ?)');
            $log->execute([$studentId, "Document credential updated to \"{$newStatus}\" by {$displayName}"]);
        }
        header('Location: student_records.php?id=' . $studentId . '&saved=1');
        exit;
    }
}

$saved = isset($_GET['saved']);

// ---- Detail view ----
if ($viewId > 0) {
    $stmt = $pdo->prepare('SELECT id, name, email, created_at FROM `user` WHERE id = ? AND role = ?');
    $stmt->execute([$viewId, 'student']);
    $student = $stmt->fetch();

    if (!$student) {
        header('Location: student_records.php');
        exit;
    }

    ensure_vault_rows($pdo, $viewId);

    $stmt = $pdo->prepare(
        'SELECT program, year_level, average_grade, academic_status, enrollment_status, completed_units, units_remaining
         FROM student_profile WHERE user_id = ?'
    );
    $stmt->execute([$viewId]);
    $profile = $stmt->fetch() ?: [
        'program' => 'Not set', 'year_level' => 'Not set', 'average_grade' => null,
        'academic_status' => 'Good Standing', 'enrollment_status' => 'Active',
        'completed_units' => 0, 'units_remaining' => 0,
    ];

    $stmt = $pdo->prepare(
        'SELECT school_year, semester, enrollment_type, status, enrolled_at
         FROM enrollments WHERE student_id = ? ORDER BY enrolled_at DESC'
    );
    $stmt->execute([$viewId]);
    $enrollmentHistory = $stmt->fetchAll();

    $stmt = $pdo->prepare(
        'SELECT id, document_type, status, submitted_at, verified_at
         FROM document_credentials WHERE student_id = ?
         ORDER BY FIELD(document_type, "Form 137","Form 138","Birth Certificate","Good Moral","Transcript from Previous School")'
    );
    $stmt->execute([$viewId]);
    $vault = $stmt->fetchAll();

    $stmt = $pdo->prepare(
        'SELECT message, created_at FROM activity_log WHERE user_id = ? ORDER BY created_at DESC LIMIT 6'
    );
    $stmt->execute([$viewId]);
    $activity = $stmt->fetchAll();

    $avgGradeDisplay = $profile['average_grade'] !== null ? number_format((float) $profile['average_grade'], 1) . '%' : 'N/A';
}

// ---- List view ----
if ($viewId === 0) {
    $q = trim($_GET['q'] ?? '');
    $sql = "SELECT u.id, u.name, u.email, sp.program, sp.year_level, sp.enrollment_status,
                   (SELECT COUNT(*) FROM document_credentials dc WHERE dc.student_id = u.id AND dc.status = 'Verified') AS verified_count,
                   (SELECT COUNT(*) FROM document_credentials dc WHERE dc.student_id = u.id) AS total_docs
            FROM `user` u
            LEFT JOIN student_profile sp ON sp.user_id = u.id
            WHERE u.role = 'student'";
    $params = [];
    if ($q !== '') {
        $sql .= ' AND (u.name LIKE ? OR u.email LIKE ?)';
        $params[] = "%$q%";
        $params[] = "%$q%";
    }
    $sql .= ' ORDER BY u.name ASC';
    $stmt = $pdo->prepare($sql);
    $stmt->execute($params);
    $students = $stmt->fetchAll();
}
?>
<!doctype html>
<html>
<head>
<meta charset="utf-8">
<title>Student Records - Registrar</title>
<link rel="stylesheet" href="styles.css">
<link rel="stylesheet" href="dashboard.css">
</head>
<body class="dashboard-body">

  <aside class="dash-sidebar">
    <div class="dash-brand"><span class="cap">🎓</span> Registrar System</div>
    <nav class="dash-nav">
      <a href="admin_dashboard.php"><span class="ic">🏠</span> Dashboard</a>
      <a href="student_records.php" class="active"><span class="ic">🗂️</span> Student Records (201 File)</a>
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

    <?php if ($viewId === 0): ?>
      <h1 class="dash-heading">🗂️ Student Academic Records (201 File &amp; Vault)</h1>
      <p class="dash-subheading">Permanent academic history, document credentials, and official status control for every student.</p>

      <div class="dash-panel">
        <form method="get" class="search-bar">
          <input type="text" name="q" placeholder="Search by name or email..." value="<?php echo htmlspecialchars($q); ?>">
          <button type="submit" class="btn-small">Search</button>
          <?php if ($q !== ''): ?><a href="student_records.php" class="btn-small secondary">Clear</a><?php endif; ?>
        </form>

        <?php if (!$students): ?>
          <div class="empty-note-block">No students found.</div>
        <?php else: ?>
          <table class="table-simple">
            <thead>
              <tr>
                <th>Name</th>
                <th>Email</th>
                <th>Program</th>
                <th>Year Level</th>
                <th>Status</th>
                <th>201 Vault</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              <?php foreach ($students as $s):
                  $status = $s['enrollment_status'] ?? 'Active';
                  $vaultLabel = ($s['total_docs'] ?? 0) > 0
                      ? $s['verified_count'] . '/' . $s['total_docs'] . ' verified'
                      : 'Not initialized';
              ?>
              <tr>
                <td><?php echo htmlspecialchars($s['name']); ?></td>
                <td><?php echo htmlspecialchars($s['email']); ?></td>
                <td><?php echo htmlspecialchars($s['program'] ?? 'Not set'); ?></td>
                <td><?php echo htmlspecialchars($s['year_level'] ?? 'Not set'); ?></td>
                <td><span class="badge <?php echo badge_class_enrollment($status); ?>"><?php echo htmlspecialchars($status); ?></span></td>
                <td><?php echo htmlspecialchars($vaultLabel); ?></td>
                <td><a class="row-link" href="student_records.php?id=<?php echo $s['id']; ?>">View / Manage ›</a></td>
              </tr>
              <?php endforeach; ?>
            </tbody>
          </table>
        <?php endif; ?>
      </div>

    <?php else: ?>

      <a href="student_records.php" class="back-link">‹ Back to Student Records</a>

      <?php if ($saved): ?>
        <div class="flash-note">Changes saved.</div>
      <?php endif; ?>

      <div class="detail-header">
        <div>
          <h1><?php echo htmlspecialchars($student['name']); ?></h1>
          <div class="meta"><?php echo htmlspecialchars($student['email']); ?> &middot; Student since <?php echo htmlspecialchars(date('M Y', strtotime($student['created_at']))); ?></div>
        </div>
        <span class="badge <?php echo badge_class_enrollment($profile['enrollment_status']); ?>" style="font-size:14px;padding:8px 16px;">
          <?php echo htmlspecialchars($profile['enrollment_status']); ?>
        </span>
      </div>

      <!-- Permanent Academic Record -->
      <div class="dash-panel" style="margin-bottom:18px;">
        <h2>📁 Permanent Academic Record</h2>
        <div class="stat-mini-row">
          <div class="stat-mini"><div class="label">Program</div><div class="value"><?php echo htmlspecialchars($profile['program']); ?></div></div>
          <div class="stat-mini"><div class="label">Year Level</div><div class="value"><?php echo htmlspecialchars($profile['year_level']); ?></div></div>
          <div class="stat-mini"><div class="label">Average Grade</div><div class="value"><?php echo htmlspecialchars($avgGradeDisplay); ?></div></div>
          <div class="stat-mini"><div class="label">Academic Standing</div><div class="value"><?php echo htmlspecialchars($profile['academic_status']); ?></div></div>
        </div>
        <div class="stat-mini-row">
          <div class="stat-mini"><div class="label">Completed Units</div><div class="value"><?php echo number_format((int) $profile['completed_units']); ?></div></div>
          <div class="stat-mini"><div class="label">Units Remaining</div><div class="value"><?php echo number_format((int) $profile['units_remaining']); ?></div></div>
        </div>

        <h2 style="margin-top:22px;">Enrollment History</h2>
        <?php if (!$enrollmentHistory): ?>
          <p class="empty-note">No enrollment records yet.</p>
        <?php else: ?>
          <table class="table-simple">
            <thead><tr><th>School Year</th><th>Semester</th><th>Type</th><th>Status</th><th>Enrolled At</th></tr></thead>
            <tbody>
              <?php foreach ($enrollmentHistory as $e): ?>
              <tr>
                <td><?php echo htmlspecialchars($e['school_year']); ?></td>
                <td><?php echo htmlspecialchars($e['semester']); ?></td>
                <td><?php echo htmlspecialchars(ucfirst($e['enrollment_type'])); ?></td>
                <td><?php echo htmlspecialchars(ucfirst($e['status'])); ?></td>
                <td><?php echo htmlspecialchars(date('M d, Y', strtotime($e['enrolled_at']))); ?></td>
              </tr>
              <?php endforeach; ?>
            </tbody>
          </table>
        <?php endif; ?>

        <h2 style="margin-top:22px;">Recent Activity</h2>
        <?php if (!$activity): ?>
          <p class="empty-note">No activity logged yet.</p>
        <?php endif; ?>
        <?php foreach ($activity as $item): ?>
          <div class="activity-row">
            <span class="activity-time"><?php echo htmlspecialchars(date('M d, h:i A', strtotime($item['created_at']))); ?></span>
            <span><?php echo htmlspecialchars($item['message']); ?></span>
          </div>
        <?php endforeach; ?>
      </div>

      <!-- Document Credentials Vault -->
      <div class="dash-panel" style="margin-bottom:18px;">
        <h2>🔐 Document Credentials Vault</h2>
        <table class="table-simple">
          <thead><tr><th>Document</th><th>Status</th><th>Submitted</th><th>Verified</th><th>Update</th></tr></thead>
          <tbody>
            <?php foreach ($vault as $doc): ?>
            <tr>
              <td><?php echo htmlspecialchars($doc['document_type']); ?></td>
              <td><span class="badge <?php echo badge_class_vault($doc['status']); ?>"><?php echo htmlspecialchars($doc['status']); ?></span></td>
              <td><?php echo $doc['submitted_at'] ? htmlspecialchars(date('M d, Y', strtotime($doc['submitted_at']))) : '—'; ?></td>
              <td><?php echo $doc['verified_at'] ? htmlspecialchars(date('M d, Y', strtotime($doc['verified_at']))) : '—'; ?></td>
              <td>
                <form method="post" class="vault-form">
                  <input type="hidden" name="action" value="update_document">
                  <input type="hidden" name="student_id" value="<?php echo $student['id']; ?>">
                  <input type="hidden" name="doc_id" value="<?php echo $doc['id']; ?>">
                  <select name="doc_status">
                    <?php foreach (['Missing', 'Submitted', 'Verified'] as $st): ?>
                      <option value="<?php echo $st; ?>" <?php echo $doc['status'] === $st ? 'selected' : ''; ?>><?php echo $st; ?></option>
                    <?php endforeach; ?>
                  </select>
                  <button type="submit" class="btn-small">Save</button>
                </form>
              </td>
            </tr>
            <?php endforeach; ?>
          </tbody>
        </table>
      </div>

      <!-- Academic Status Control -->
      <div class="dash-panel">
        <h2>⚙️ Academic Status Control</h2>
        <p class="dash-subheading" style="margin-top:0;">Set the student's official standing (separate from academic/GPA standing above).</p>
        <form method="post" class="status-control-form">
          <input type="hidden" name="action" value="update_enrollment_status">
          <input type="hidden" name="student_id" value="<?php echo $student['id']; ?>">
          <select name="enrollment_status">
            <?php foreach (ENROLLMENT_STATUSES as $st): ?>
              <option value="<?php echo $st; ?>" <?php echo $profile['enrollment_status'] === $st ? 'selected' : ''; ?>><?php echo $st; ?></option>
            <?php endforeach; ?>
          </select>
          <button type="submit" class="btn-small">Update Status</button>
        </form>
      </div>

    <?php endif; ?>

    </div>
  </div>

</body>
</html>