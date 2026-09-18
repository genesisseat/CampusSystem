<?php
require 'config.php';
$displayName = $_SESSION['user_name'] ?? 'Registrar';

/**
 * This module reads/writes the tables added in
 * enrollment_validation_schema.sql: enrolled_subjects, add_drop_requests,
 * overload_waiver_requests, plus the existing enrollments/user tables.
 * Run that migration against registrar_db before using this page.
 */

$currentSchoolYear = $pdo->query("SELECT `value` FROM settings WHERE `key` = 'current_school_year'")->fetchColumn() ?: '';
$currentSemester   = $pdo->query("SELECT `value` FROM settings WHERE `key` = 'current_semester'")->fetchColumn() ?: '';

$tab = $_GET['tab'] ?? 'registration';
if (!in_array($tab, ['registration', 'adddrop', 'overload'], true)) {
    $tab = 'registration';
}

$flash = null;
$flashType = 'ok'; // ok | error

function log_activity(PDO $pdo, int $userId, string $message): void
{
    $stmt = $pdo->prepare("INSERT INTO activity_log (user_id, message, created_at) VALUES (?, ?, NOW())");
    $stmt->execute([$userId, $message]);
}

if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $action = $_POST['action'] ?? '';
    $registrarId = (int) $_SESSION['user_id'];

    try {
        // ---- Final Registration Approval ----
        if ($action === 'approve_enrollment' || $action === 'reject_enrollment') {
            $enrollmentId = (int) ($_POST['enrollment_id'] ?? 0);
            $newStatus = $action === 'approve_enrollment' ? 'active' : 'rejected';

            $stmt = $pdo->prepare("SELECT student_id FROM enrollments WHERE id = ? AND status = 'pending'");
            $stmt->execute([$enrollmentId]);
            $row = $stmt->fetch();

            if ($row) {
                $upd = $pdo->prepare("UPDATE enrollments SET status = ? WHERE id = ?");
                $upd->execute([$newStatus, $enrollmentId]);

                $verb = $newStatus === 'active' ? 'approved' : 'rejected';
                log_activity($pdo, $row['student_id'], "Enrollment {$verb} by registrar for {$currentSchoolYear} {$currentSemester}.");
                $flash = "Enrollment #{$enrollmentId} has been {$verb}.";
            } else {
                $flash = 'That enrollment is no longer pending.';
                $flashType = 'error';
            }
        }

        // ---- Add / Drop / Change processing ----
        elseif ($action === 'approve_adddrop' || $action === 'reject_adddrop') {
            $reqId = (int) ($_POST['request_id'] ?? 0);

            $stmt = $pdo->prepare("SELECT * FROM add_drop_requests WHERE id = ? AND status = 'pending'");
            $stmt->execute([$reqId]);
            $req = $stmt->fetch();

            if (!$req) {
                $flash = 'That request is no longer pending.';
                $flashType = 'error';
            } elseif ($action === 'reject_adddrop') {
                $upd = $pdo->prepare("UPDATE add_drop_requests SET status = 'rejected', processed_at = NOW(), processed_by = ? WHERE id = ?");
                $upd->execute([$registrarId, $reqId]);
                log_activity($pdo, $req['student_id'], ucfirst($req['request_type']) . ' request rejected by registrar.');
                $flash = 'Request rejected.';
            } else {
                $pdo->beginTransaction();

                if (in_array($req['request_type'], ['drop', 'change'], true) && $req['class_offering_id']) {
                    $upd = $pdo->prepare(
                        "UPDATE enrolled_subjects SET status = 'dropped'
                         WHERE enrollment_id = ? AND class_offering_id = ? AND status = 'enrolled'"
                    );
                    $upd->execute([$req['enrollment_id'], $req['class_offering_id']]);
                    $pdo->prepare("UPDATE class_offerings SET slots_taken = GREATEST(slots_taken - 1, 0) WHERE id = ?")
                        ->execute([$req['class_offering_id']]);
                }

                if (in_array($req['request_type'], ['add', 'change'], true) && $req['target_class_offering_id']) {
                    $ins = $pdo->prepare(
                        "INSERT INTO enrolled_subjects (enrollment_id, class_offering_id, status) VALUES (?, ?, 'enrolled')"
                    );
                    $ins->execute([$req['enrollment_id'], $req['target_class_offering_id']]);
                    $pdo->prepare("UPDATE class_offerings SET slots_taken = slots_taken + 1 WHERE id = ?")
                        ->execute([$req['target_class_offering_id']]);
                }

                $upd = $pdo->prepare("UPDATE add_drop_requests SET status = 'approved', processed_at = NOW(), processed_by = ? WHERE id = ?");
                $upd->execute([$registrarId, $reqId]);

                $pdo->commit();
                log_activity($pdo, $req['student_id'], ucfirst($req['request_type']) . ' request approved by registrar.');
                $flash = 'Request approved and subjects updated.';
            }
        }

        // ---- Overload & Waiver approvals ----
        elseif ($action === 'approve_overload' || $action === 'reject_overload') {
            $reqId = (int) ($_POST['request_id'] ?? 0);
            $newStatus = $action === 'approve_overload' ? 'approved' : 'rejected';

            $stmt = $pdo->prepare("SELECT student_id, request_type FROM overload_waiver_requests WHERE id = ? AND status = 'pending'");
            $stmt->execute([$reqId]);
            $req = $stmt->fetch();

            if ($req) {
                $upd = $pdo->prepare("UPDATE overload_waiver_requests SET status = ?, processed_at = NOW(), processed_by = ? WHERE id = ?");
                $upd->execute([$newStatus, $registrarId, $reqId]);
                log_activity($pdo, $req['student_id'], ucfirst($req['request_type']) . " request {$newStatus} by registrar.");
                $flash = ucfirst($req['request_type']) . " request {$newStatus}.";
            } else {
                $flash = 'That request is no longer pending.';
                $flashType = 'error';
            }
        }
    } catch (Throwable $e) {
        if ($pdo->inTransaction()) {
            $pdo->rollBack();
        }
        $flash = 'Something went wrong processing that request.';
        $flashType = 'error';
    }

    // PRG pattern: redirect back to the same tab so refresh doesn't resubmit.
    $_SESSION['ev_flash'] = $flash;
    $_SESSION['ev_flash_type'] = $flashType;
    header('Location: enrollment_validation.php?tab=' . $tab);
    exit;
}

// Pick up flash message set by a previous POST redirect
if (isset($_SESSION['ev_flash'])) {
    $flash = $_SESSION['ev_flash'];
    $flashType = $_SESSION['ev_flash_type'] ?? 'ok';
    unset($_SESSION['ev_flash'], $_SESSION['ev_flash_type']);
}

// ---- Data for the active tab ----

$pendingEnrollments = [];
$adddropRequests = [];
$overloadRequests = [];

if ($tab === 'registration') {
    $pendingEnrollments = $pdo->prepare(
        "SELECT e.id, e.school_year, e.semester, e.enrollment_type,
                u.name, sp.program, sp.year_level
         FROM enrollments e
         JOIN `user` u ON u.id = e.student_id
         LEFT JOIN student_profile sp ON sp.user_id = e.student_id
         WHERE e.status = 'pending'
         ORDER BY e.id ASC"
    );
    $pendingEnrollments->execute();
    $pendingEnrollments = $pendingEnrollments->fetchAll();
} elseif ($tab === 'adddrop') {
    $adddropRequests = $pdo->query(
        "SELECT adr.id, adr.request_type, adr.reason, adr.requested_at,
                u.name,
                fs.subject_code AS from_code, co.section_code AS from_section,
                ts.subject_code AS to_code, tco.section_code AS to_section
         FROM add_drop_requests adr
         JOIN `user` u ON u.id = adr.student_id
         LEFT JOIN class_offerings co ON co.id = adr.class_offering_id
         LEFT JOIN subjects fs ON fs.id = co.subject_id
         LEFT JOIN class_offerings tco ON tco.id = adr.target_class_offering_id
         LEFT JOIN subjects ts ON ts.id = tco.subject_id
         WHERE adr.status = 'pending'
         ORDER BY adr.requested_at ASC"
    )->fetchAll();
} else {
    $overloadRequests = $pdo->query(
        "SELECT owr.id, owr.request_type, owr.requested_units, owr.reason, owr.requested_at,
                u.name, s.subject_code, s.subject_name
         FROM overload_waiver_requests owr
         JOIN `user` u ON u.id = owr.student_id
         LEFT JOIN subjects s ON s.id = owr.subject_id
         WHERE owr.status = 'pending'
         ORDER BY owr.requested_at ASC"
    )->fetchAll();
}
?>
<!doctype html>
<html>
<head>
<meta charset="utf-8">
<title>Enrollment & Subject Validation - Registrar</title>
<link rel="stylesheet" href="styles.css">
<link rel="stylesheet" href="dashboard.css">
<style>
  .ev-tabs { display: flex; gap: 8px; margin-bottom: 18px; }
  .ev-tab {
    padding: 9px 16px; border-radius: 8px; text-decoration: none;
    font-weight: 700; font-size: 13px; color: #14213d; background: #e7edf6;
  }
  .ev-tab.active { background: #2f6feb; color: #fff; }
  .ev-actions { display: flex; gap: 8px; }
  .btn-reject { background: #d64545; }
  form.inline { display: inline; }
</style>
</head>
<body class="dashboard-body">

  <aside class="dash-sidebar">
    <div class="dash-brand"><span class="cap">🎓</span> Registrar System</div>
    <nav class="dash-nav">
      <a href="admin_dashboard.php"><span class="ic">🏠</span> Dashboard</a>
      <a href="student_records.php"><span class="ic">🗂️</span> Student Records (201 File)</a>
      <a href="enrollment_validation.php" class="active"><span class="ic">📝</span> Enrollment &amp; Validation</a>
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
      <h1 class="dash-heading">📝 Enrollment &amp; Subject Validation</h1>
      <p class="dash-subheading">Approve official registrations and process add/drop/change and overload requests. Current term: <?php echo htmlspecialchars($currentSemester . ' ' . $currentSchoolYear); ?></p>

      <?php if ($flash): ?>
        <div class="flash-note" style="<?php echo $flashType === 'error' ? 'background:#fdeaea;color:#d64545;border-color:rgba(214,69,69,0.25);' : ''; ?>">
          <?php echo htmlspecialchars($flash); ?>
        </div>
      <?php endif; ?>

      <div class="ev-tabs">
        <a href="?tab=registration" class="ev-tab <?php echo $tab === 'registration' ? 'active' : ''; ?>">Final Registration Approval</a>
        <a href="?tab=adddrop" class="ev-tab <?php echo $tab === 'adddrop' ? 'active' : ''; ?>">Add / Drop / Change</a>
        <a href="?tab=overload" class="ev-tab <?php echo $tab === 'overload' ? 'active' : ''; ?>">Overload &amp; Waiver</a>
      </div>

      <div class="dash-panel">

        <?php if ($tab === 'registration'): ?>
          <h2>Pending Enrollments</h2>
          <?php if (!$pendingEnrollments): ?>
            <p class="empty-note">No pending enrollments to review.</p>
          <?php else: ?>
            <table class="table-simple">
              <thead><tr><th>Student</th><th>Program / Year</th><th>Term</th><th>Type</th><th>Action</th></tr></thead>
              <tbody>
                <?php foreach ($pendingEnrollments as $e): ?>
                <tr>
                  <td><?php echo htmlspecialchars($e['name']); ?></td>
                  <td><?php echo htmlspecialchars(($e['program'] ?? 'N/A') . ' — ' . ($e['year_level'] ?? 'N/A')); ?></td>
                  <td><?php echo htmlspecialchars($e['semester'] . ' ' . $e['school_year']); ?></td>
                  <td><?php echo htmlspecialchars(ucfirst($e['enrollment_type'])); ?></td>
                  <td class="ev-actions">
                    <form class="inline" method="post">
                      <input type="hidden" name="action" value="approve_enrollment">
                      <input type="hidden" name="enrollment_id" value="<?php echo (int) $e['id']; ?>">
                      <button class="btn-small" type="submit">Approve</button>
                    </form>
                    <form class="inline" method="post">
                      <input type="hidden" name="action" value="reject_enrollment">
                      <input type="hidden" name="enrollment_id" value="<?php echo (int) $e['id']; ?>">
                      <button class="btn-small btn-reject" type="submit">Reject</button>
                    </form>
                  </td>
                </tr>
                <?php endforeach; ?>
              </tbody>
            </table>
          <?php endif; ?>

        <?php elseif ($tab === 'adddrop'): ?>
          <h2>Add / Drop / Change Requests</h2>
          <?php if (!$adddropRequests): ?>
            <p class="empty-note">No pending add/drop/change requests.</p>
          <?php else: ?>
            <table class="table-simple">
              <thead><tr><th>Student</th><th>Type</th><th>From</th><th>To</th><th>Reason</th><th>Submitted</th><th>Action</th></tr></thead>
              <tbody>
                <?php foreach ($adddropRequests as $r): ?>
                <tr>
                  <td><?php echo htmlspecialchars($r['name']); ?></td>
                  <td><span class="badge badge-on-leave"><?php echo htmlspecialchars(ucfirst($r['request_type'])); ?></span></td>
                  <td><?php echo $r['from_code'] ? htmlspecialchars($r['from_code'] . ' (' . $r['from_section'] . ')') : '—'; ?></td>
                  <td><?php echo $r['to_code'] ? htmlspecialchars($r['to_code'] . ' (' . $r['to_section'] . ')') : '—'; ?></td>
                  <td><?php echo htmlspecialchars($r['reason'] ?? '—'); ?></td>
                  <td><?php echo htmlspecialchars(date('M d, Y', strtotime($r['requested_at']))); ?></td>
                  <td class="ev-actions">
                    <form class="inline" method="post">
                      <input type="hidden" name="action" value="approve_adddrop">
                      <input type="hidden" name="request_id" value="<?php echo (int) $r['id']; ?>">
                      <button class="btn-small" type="submit">Approve</button>
                    </form>
                    <form class="inline" method="post">
                      <input type="hidden" name="action" value="reject_adddrop">
                      <input type="hidden" name="request_id" value="<?php echo (int) $r['id']; ?>">
                      <button class="btn-small btn-reject" type="submit">Reject</button>
                    </form>
                  </td>
                </tr>
                <?php endforeach; ?>
              </tbody>
            </table>
          <?php endif; ?>

        <?php else: ?>
          <h2>Overload &amp; Waiver Requests</h2>
          <?php if (!$overloadRequests): ?>
            <p class="empty-note">No pending overload or waiver requests.</p>
          <?php else: ?>
            <table class="table-simple">
              <thead><tr><th>Student</th><th>Type</th><th>Details</th><th>Reason</th><th>Submitted</th><th>Action</th></tr></thead>
              <tbody>
                <?php foreach ($overloadRequests as $r):
                  $details = $r['request_type'] === 'overload'
                      ? ($r['requested_units'] !== null ? htmlspecialchars($r['requested_units']) . ' units' : '—')
                      : htmlspecialchars(trim(($r['subject_code'] ?? '') . ' ' . ($r['subject_name'] ?? '')) ?: '—');
                ?>
                <tr>
                  <td><?php echo htmlspecialchars($r['name']); ?></td>
                  <td><span class="badge badge-on-leave"><?php echo htmlspecialchars(ucfirst($r['request_type'])); ?></span></td>
                  <td><?php echo $details; ?></td>
                  <td><?php echo htmlspecialchars($r['reason'] ?? '—'); ?></td>
                  <td><?php echo htmlspecialchars(date('M d, Y', strtotime($r['requested_at']))); ?></td>
                  <td class="ev-actions">
                    <form class="inline" method="post">
                      <input type="hidden" name="action" value="approve_overload">
                      <input type="hidden" name="request_id" value="<?php echo (int) $r['id']; ?>">
                      <button class="btn-small" type="submit">Approve</button>
                    </form>
                    <form class="inline" method="post">
                      <input type="hidden" name="action" value="reject_overload">
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