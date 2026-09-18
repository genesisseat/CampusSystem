<?php
require 'config.php';
$displayName = $_SESSION['user_name'] ?? 'Registrar';

/**
 * Reads/writes `grades` and `completion_revision_requests`
 * (grade_control_schema.sql), plus the `settings` keys
 * grade_encoding_open / grade_encoding_deadline for window control.
 * Registrar encodes grades directly here (no separate faculty role).
 */

$currentSchoolYear = $pdo->query("SELECT `value` FROM settings WHERE `key` = 'current_school_year'")->fetchColumn() ?: '';
$currentSemester   = $pdo->query("SELECT `value` FROM settings WHERE `key` = 'current_semester'")->fetchColumn() ?: '';

function get_setting(PDO $pdo, string $key, string $default = ''): string
{
    $stmt = $pdo->prepare("SELECT `value` FROM settings WHERE `key` = ?");
    $stmt->execute([$key]);
    $val = $stmt->fetchColumn();
    return $val !== false ? $val : $default;
}

function set_setting(PDO $pdo, string $key, string $value): void
{
    $stmt = $pdo->prepare(
        "INSERT INTO settings (`key`, `value`) VALUES (?, ?)
         ON DUPLICATE KEY UPDATE `value` = VALUES(`value`)"
    );
    $stmt->execute([$key, $value]);
}

function log_activity(PDO $pdo, int $userId, string $message): void
{
    $stmt = $pdo->prepare("INSERT INTO activity_log (user_id, message, created_at) VALUES (?, ?, NOW())");
    $stmt->execute([$userId, $message]);
}

$tab = $_GET['tab'] ?? 'encoding';
if (!in_array($tab, ['encoding', 'verify', 'requests'], true)) {
    $tab = 'encoding';
}

$flash = null;
$flashType = 'ok';
if (isset($_SESSION['gc_flash'])) {
    $flash = $_SESSION['gc_flash'];
    $flashType = $_SESSION['gc_flash_type'] ?? 'ok';
    unset($_SESSION['gc_flash'], $_SESSION['gc_flash_type']);
}

if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $action = $_POST['action'] ?? '';
    $registrarId = (int) $_SESSION['user_id'];

    try {
        // ---- Window control ----
        if ($action === 'save_window') {
            $open = isset($_POST['window_open']) ? '1' : '0';
            $deadline = trim($_POST['deadline'] ?? '');
            set_setting($pdo, 'grade_encoding_open', $open);
            set_setting($pdo, 'grade_encoding_deadline', $deadline);
            $flash = 'Encoding window settings saved.';
        }

        // ---- Encode / update a grade (only while window is open) ----
        elseif ($action === 'save_grade') {
            if (get_setting($pdo, 'grade_encoding_open', '0') !== '1') {
                $flash = 'Grade encoding window is closed.';
                $flashType = 'error';
            } else {
                $enrolledSubjectId = (int) ($_POST['enrolled_subject_id'] ?? 0);
                $isInc = isset($_POST['is_inc']) ? 1 : 0;
                $gradeVal = $_POST['grade'] ?? '';

                if (!$isInc && ($gradeVal === '' || (float) $gradeVal < 1.0 || (float) $gradeVal > 5.0)) {
                    $flash = 'Grade must be between 1.0 and 5.0, or mark as INC.';
                    $flashType = 'error';
                } else {
                    $gradeToStore = $isInc ? null : (float) $gradeVal;

                    $stmt = $pdo->prepare("SELECT id FROM grades WHERE enrolled_subject_id = ?");
                    $stmt->execute([$enrolledSubjectId]);
                    $existing = $stmt->fetchColumn();

                    if ($existing) {
                        $upd = $pdo->prepare(
                            "UPDATE grades SET grade = ?, is_inc = ?, status = 'draft',
                             encoded_by = ?, encoded_at = NOW() WHERE id = ?"
                        );
                        $upd->execute([$gradeToStore, $isInc, $registrarId, $existing]);
                    } else {
                        $ins = $pdo->prepare(
                            "INSERT INTO grades (enrolled_subject_id, grade, is_inc, status, encoded_by, encoded_at)
                             VALUES (?, ?, ?, 'draft', ?, NOW())"
                        );
                        $ins->execute([$enrolledSubjectId, $gradeToStore, $isInc, $registrarId]);
                    }
                    $flash = 'Grade saved as draft.';
                }
            }
        }

        // ---- Verify or lock a grade ----
        elseif ($action === 'verify_grade') {
            $id = (int) ($_POST['grade_id'] ?? 0);
            $stmt = $pdo->prepare("UPDATE grades SET status = 'verified', verified_by = ?, verified_at = NOW() WHERE id = ? AND status = 'draft'");
            $stmt->execute([$registrarId, $id]);
            $flash = 'Grade verified.';
        } elseif ($action === 'lock_grade') {
            $id = (int) ($_POST['grade_id'] ?? 0);
            $stmt = $pdo->prepare("UPDATE grades SET status = 'locked', locked_at = NOW() WHERE id = ? AND status = 'verified'");
            $stmt->execute([$id]);
            $flash = 'Grade locked.';
        }

        // ---- Completion / revision requests ----
        elseif ($action === 'approve_request' || $action === 'reject_request') {
            $reqId = (int) ($_POST['request_id'] ?? 0);
            $stmt = $pdo->prepare("SELECT * FROM completion_revision_requests WHERE id = ? AND status = 'pending'");
            $stmt->execute([$reqId]);
            $req = $stmt->fetch();

            if (!$req) {
                $flash = 'That request is no longer pending.';
                $flashType = 'error';
            } elseif ($action === 'reject_request') {
                $upd = $pdo->prepare("UPDATE completion_revision_requests SET status = 'rejected', processed_at = NOW(), processed_by = ? WHERE id = ?");
                $upd->execute([$registrarId, $reqId]);
                log_activity($pdo, $req['student_id'], ucfirst($req['request_type']) . ' request rejected by registrar.');
                $flash = 'Request rejected.';
            } else {
                $upd = $pdo->prepare(
                    "UPDATE grades SET grade = ?, is_inc = 0, status = 'verified',
                     verified_by = ?, verified_at = NOW() WHERE id = ?"
                );
                $upd->execute([$req['requested_grade'], $registrarId, $req['grade_id']]);

                $upd2 = $pdo->prepare("UPDATE completion_revision_requests SET status = 'approved', processed_at = NOW(), processed_by = ? WHERE id = ?");
                $upd2->execute([$registrarId, $reqId]);

                log_activity($pdo, $req['student_id'], ucfirst($req['request_type']) . ' request approved by registrar.');
                $flash = 'Request approved and grade updated.';
            }
        }
    } catch (Throwable $e) {
        $flash = 'Something went wrong: ' . $e->getMessage();
        $flashType = 'error';
    }

    $_SESSION['gc_flash'] = $flash;
    $_SESSION['gc_flash_type'] = $flashType;
    header('Location: grade_control.php?tab=' . $tab);
    exit;
}

$windowOpen = get_setting($pdo, 'grade_encoding_open', '0') === '1';
$windowDeadline = get_setting($pdo, 'grade_encoding_deadline', '');

$pendingEncoding = [];
$draftAndVerified = [];
$pendingRequests = [];

if ($tab === 'encoding') {
    $pendingEncoding = $pdo->prepare(
        "SELECT es.id AS enrolled_subject_id, u.name, s.subject_code, s.subject_name, co.section_code,
                g.id AS grade_id, g.grade, g.is_inc
         FROM enrolled_subjects es
         JOIN enrollments e ON e.id = es.enrollment_id
         JOIN `user` u ON u.id = e.student_id
         JOIN class_offerings co ON co.id = es.class_offering_id
         JOIN subjects s ON s.id = co.subject_id
         LEFT JOIN grades g ON g.enrolled_subject_id = es.id
         WHERE es.status = 'enrolled' AND co.school_year = ? AND co.semester = ?
           AND (g.id IS NULL OR g.status = 'draft')
         ORDER BY u.name, s.subject_code"
    );
    $pendingEncoding->execute([$currentSchoolYear, $currentSemester]);
    $pendingEncoding = $pendingEncoding->fetchAll();
} elseif ($tab === 'verify') {
    $draftAndVerified = $pdo->query(
        "SELECT g.id, g.grade, g.is_inc, g.status,
                u.name, s.subject_code, s.subject_name, co.section_code
         FROM grades g
         JOIN enrolled_subjects es ON es.id = g.enrolled_subject_id
         JOIN enrollments e ON e.id = es.enrollment_id
         JOIN `user` u ON u.id = e.student_id
         JOIN class_offerings co ON co.id = es.class_offering_id
         JOIN subjects s ON s.id = co.subject_id
         WHERE g.status IN ('draft','verified')
         ORDER BY FIELD(g.status, 'draft','verified'), u.name"
    )->fetchAll();
} else {
    $pendingRequests = $pdo->query(
        "SELECT cr.id, cr.request_type, cr.requested_grade, cr.reason, cr.requested_at,
                u.name, s.subject_code, s.subject_name, g.grade AS current_grade, g.is_inc AS current_is_inc
         FROM completion_revision_requests cr
         JOIN `user` u ON u.id = cr.student_id
         JOIN grades g ON g.id = cr.grade_id
         JOIN enrolled_subjects es ON es.id = g.enrolled_subject_id
         JOIN class_offerings co ON co.id = es.class_offering_id
         JOIN subjects s ON s.id = co.subject_id
         WHERE cr.status = 'pending'
         ORDER BY cr.requested_at ASC"
    )->fetchAll();
}
?>
<!doctype html>
<html>
<head>
<meta charset="utf-8">
<title>Grade Control & Archival - Registrar</title>
<link rel="stylesheet" href="styles.css">
<link rel="stylesheet" href="dashboard.css">
<style>
  .gc-tabs { display: flex; gap: 8px; margin-bottom: 18px; }
  .gc-tab { padding: 9px 16px; border-radius: 8px; text-decoration: none; font-weight: 700; font-size: 13px; color: #14213d; background: #e7edf6; }
  .gc-tab.active { background: #2f6feb; color: #fff; }
  .gc-window-form { display: flex; align-items: center; gap: 16px; flex-wrap: wrap; }
  .gc-window-form label { font-size: 13px; font-weight: 600; color: #33475b; display: flex; align-items: center; gap: 6px; }
  .gc-window-form input[type="date"] { padding: 8px 10px; border-radius: 8px; border: 1px solid #d7dee8; }
  .grade-input-row { display: flex; align-items: center; gap: 8px; }
  .grade-input-row input[type="number"] { width: 70px; padding: 7px; border-radius: 6px; border: 1px solid #d7dee8; }
  .grade-input-row label { font-size: 12px; color: #5c6b7f; display: flex; align-items: center; gap: 4px; }
  form.inline { display: inline; }
  .btn-reject { background: #d64545; }
  .window-pill { padding: 4px 10px; border-radius: 999px; font-size: 12px; font-weight: 700; }
  .window-open { background: #e6f8ee; color: #1eab5c; }
  .window-closed { background: #fdeaea; color: #d64545; }
</style>
</head>
<body class="dashboard-body">

  <aside class="dash-sidebar">
    <div class="dash-brand"><span class="cap">🎓</span> Registrar System</div>
    <nav class="dash-nav">
      <a href="admin_dashboard.php"><span class="ic">🏠</span> Dashboard</a>
      <a href="student_records.php"><span class="ic">🗂️</span> Student Records (201 File)</a>
      <a href="enrollment_validation.php"><span class="ic">📝</span> Enrollment &amp; Validation</a>
      <a href="class_scheduling.php"><span class="ic">🏫</span> Class Scheduling</a>
      <a href="grade_control.php" class="active"><span class="ic">📊</span> Grade Control</a>
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
      <h1 class="dash-heading">📊 Grade Control &amp; Archival</h1>
      <p class="dash-subheading">Control the encoding window, verify and lock grade sheets, and process completion/revision requests for <?php echo htmlspecialchars($currentSemester . ' ' . $currentSchoolYear); ?>.</p>

      <?php if ($flash): ?>
        <div class="flash-note" style="<?php echo $flashType === 'error' ? 'background:#fdeaea;color:#d64545;border-color:rgba(214,69,69,0.25);' : ''; ?>">
          <?php echo htmlspecialchars($flash); ?>
        </div>
      <?php endif; ?>

      <div class="dash-panel" style="margin-bottom:18px;">
        <h2>Encoding Window <span class="window-pill <?php echo $windowOpen ? 'window-open' : 'window-closed'; ?>"><?php echo $windowOpen ? 'OPEN' : 'CLOSED'; ?></span></h2>
        <form method="post" class="gc-window-form">
          <input type="hidden" name="action" value="save_window">
          <label><input type="checkbox" name="window_open" <?php echo $windowOpen ? 'checked' : ''; ?>> Encoding window is open</label>
          <label>Deadline: <input type="date" name="deadline" value="<?php echo htmlspecialchars($windowDeadline); ?>"></label>
          <button class="btn-small" type="submit">Save</button>
        </form>
      </div>

      <div class="gc-tabs">
        <a href="?tab=encoding" class="gc-tab <?php echo $tab === 'encoding' ? 'active' : ''; ?>">Grade Encoding</a>
        <a href="?tab=verify" class="gc-tab <?php echo $tab === 'verify' ? 'active' : ''; ?>">Verify &amp; Lock</a>
        <a href="?tab=requests" class="gc-tab <?php echo $tab === 'requests' ? 'active' : ''; ?>">Completion &amp; Revision Requests</a>
      </div>

      <div class="dash-panel">

        <?php if ($tab === 'encoding'): ?>
          <h2>Encode Grades</h2>
          <?php if (!$windowOpen): ?>
            <div class="placeholder-note">🔒 Encoding window is closed — open it above to enable saving.</div>
          <?php endif; ?>
          <?php if (!$pendingEncoding): ?>
            <p class="empty-note">Nothing left to encode for this term.</p>
          <?php else: ?>
            <table class="table-simple">
              <thead><tr><th>Student</th><th>Subject</th><th>Section</th><th>Grade</th><th></th></tr></thead>
              <tbody>
                <?php foreach ($pendingEncoding as $row): ?>
                <tr>
                  <td><?php echo htmlspecialchars($row['name']); ?></td>
                  <td><?php echo htmlspecialchars($row['subject_code'] . ' — ' . $row['subject_name']); ?></td>
                  <td><?php echo htmlspecialchars($row['section_code']); ?></td>
                  <td colspan="2">
                    <form method="post" class="grade-input-row">
                      <input type="hidden" name="action" value="save_grade">
                      <input type="hidden" name="enrolled_subject_id" value="<?php echo (int) $row['enrolled_subject_id']; ?>">
                      <input type="number" step="0.25" min="1.0" max="5.0" name="grade"
                             value="<?php echo htmlspecialchars($row['grade'] ?? ''); ?>" <?php echo !$windowOpen ? 'disabled' : ''; ?>>
                      <label><input type="checkbox" name="is_inc" <?php echo $row['is_inc'] ? 'checked' : ''; ?> <?php echo !$windowOpen ? 'disabled' : ''; ?>> INC</label>
                      <button class="btn-small" type="submit" <?php echo !$windowOpen ? 'disabled' : ''; ?>>Save</button>
                    </form>
                  </td>
                </tr>
                <?php endforeach; ?>
              </tbody>
            </table>
          <?php endif; ?>

        <?php elseif ($tab === 'verify'): ?>
          <h2>Verify &amp; Lock Grade Sheets</h2>
          <?php if (!$draftAndVerified): ?>
            <p class="empty-note">No draft or verified grades waiting to be locked.</p>
          <?php else: ?>
            <table class="table-simple">
              <thead><tr><th>Student</th><th>Subject</th><th>Section</th><th>Grade</th><th>Status</th><th>Action</th></tr></thead>
              <tbody>
                <?php foreach ($draftAndVerified as $g): ?>
                <tr>
                  <td><?php echo htmlspecialchars($g['name']); ?></td>
                  <td><?php echo htmlspecialchars($g['subject_code'] . ' — ' . $g['subject_name']); ?></td>
                  <td><?php echo htmlspecialchars($g['section_code']); ?></td>
                  <td><?php echo $g['is_inc'] ? 'INC' : htmlspecialchars(number_format((float) $g['grade'], 2)); ?></td>
                  <td><span class="badge <?php echo $g['status'] === 'draft' ? 'badge-on-leave' : 'badge-verified'; ?>"><?php echo htmlspecialchars(ucfirst($g['status'])); ?></span></td>
                  <td>
                    <?php if ($g['status'] === 'draft'): ?>
                      <form class="inline" method="post">
                        <input type="hidden" name="action" value="verify_grade">
                        <input type="hidden" name="grade_id" value="<?php echo (int) $g['id']; ?>">
                        <button class="btn-small" type="submit">Verify</button>
                      </form>
                    <?php else: ?>
                      <form class="inline" method="post">
                        <input type="hidden" name="action" value="lock_grade">
                        <input type="hidden" name="grade_id" value="<?php echo (int) $g['id']; ?>">
                        <button class="btn-small secondary" type="submit">Lock</button>
                      </form>
                    <?php endif; ?>
                  </td>
                </tr>
                <?php endforeach; ?>
              </tbody>
            </table>
          <?php endif; ?>

        <?php else: ?>
          <h2>Completion &amp; Revision Requests</h2>
          <?php if (!$pendingRequests): ?>
            <p class="empty-note">No pending completion or revision requests.</p>
          <?php else: ?>
            <table class="table-simple">
              <thead><tr><th>Student</th><th>Subject</th><th>Type</th><th>Current</th><th>Requested</th><th>Reason</th><th>Action</th></tr></thead>
              <tbody>
                <?php foreach ($pendingRequests as $r): ?>
                <tr>
                  <td><?php echo htmlspecialchars($r['name']); ?></td>
                  <td><?php echo htmlspecialchars($r['subject_code']); ?></td>
                  <td><span class="badge badge-on-leave"><?php echo htmlspecialchars(ucfirst($r['request_type'])); ?></span></td>
                  <td><?php echo $r['current_is_inc'] ? 'INC' : htmlspecialchars(number_format((float) $r['current_grade'], 2)); ?></td>
                  <td><?php echo htmlspecialchars(number_format((float) $r['requested_grade'], 2)); ?></td>
                  <td><?php echo htmlspecialchars($r['reason'] ?? '—'); ?></td>
                  <td class="ev-actions">
                    <form class="inline" method="post">
                      <input type="hidden" name="action" value="approve_request">
                      <input type="hidden" name="request_id" value="<?php echo (int) $r['id']; ?>">
                      <button class="btn-small" type="submit">Approve</button>
                    </form>
                    <form class="inline" method="post">
                      <input type="hidden" name="action" value="reject_request">
                      <input type="hidden" name="request_id" value="<?php echo (int) $r['id']; ?>">
                      <button class="btn-small btn-reject" type="submit">Reject</button>
                    </form>
                  </td>
                </tr>
                <?php endforeach; ?>
              </tbody>
            </table>
          <?php endif; ?>
        <?php endif; ?>

      </div>
    </div>
  </div>

</body>
</html>