<?php
require 'config.php';

$currentSchoolYear = get_setting($pdo, 'current_school_year', '2025-2026');
$currentSemester   = get_setting($pdo, 'current_semester', '1st Semester');
$displayName       = $_SESSION['user_name'] ?? 'Registrar';
$registrarId       = $_SESSION['user_id'] ?? 1;

$tab = $_GET['tab'] ?? 'registration';
if (!in_array($tab, ['registration', 'adddrop', 'overload', 'active_list'], true)) {
    $tab = 'registration';
}

$flash = get_flash();

// Handle Form Submissions
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $action = $_POST['action'] ?? '';

    try {
        // 1. Approve / Reject Initial Enrollment
        if ($action === 'approve_enrollment' || $action === 'reject_enrollment') {
            $enrollmentId = (int) ($_POST['enrollment_id'] ?? 0);
            $newStatus = ($action === 'approve_enrollment') ? 'active' : 'rejected';

            $stmt = $pdo->prepare("SELECT student_id, school_year, semester FROM enrollments WHERE id = ? AND status = 'pending'");
            $stmt->execute([$enrollmentId]);
            $enr = $stmt->fetch();

            if ($enr) {
                $pdo->beginTransaction();
                $upd = $pdo->prepare("UPDATE enrollments SET status = ? WHERE id = ?");
                $upd->execute([$newStatus, $enrollmentId]);

                // Also update student_profile enrollment_status
                if ($newStatus === 'active') {
                    $pdo->prepare("UPDATE student_profile SET enrollment_status = 'Active' WHERE user_id = ?")->execute([$enr['student_id']]);
                }

                $verb = ($newStatus === 'active') ? 'approved and officially validated' : 'rejected';
                log_activity($pdo, $enr['student_id'], "Enrollment for AY {$enr['school_year']} {$enr['semester']} {$verb} by {$displayName}.");
                $pdo->commit();

                set_flash("Enrollment #{$enrollmentId} has been {$verb}.");
            } else {
                set_flash("That enrollment record is no longer pending.", 'error');
            }
        }

        // 2. Add / Drop Processing
        elseif ($action === 'approve_adddrop' || $action === 'reject_adddrop') {
            $reqId = (int) ($_POST['request_id'] ?? 0);

            $stmt = $pdo->prepare("SELECT * FROM add_drop_requests WHERE id = ? AND status = 'pending'");
            $stmt->execute([$reqId]);
            $req = $stmt->fetch();

            if (!$req) {
                set_flash("That petition is no longer pending.", 'error');
            } elseif ($action === 'reject_adddrop') {
                $upd = $pdo->prepare("UPDATE add_drop_requests SET status = 'rejected', processed_at = NOW(), processed_by = ? WHERE id = ?");
                $upd->execute([$registrarId, $reqId]);
                log_activity($pdo, $req['student_id'], ucfirst($req['request_type']) . " petition rejected by registrar.");
                set_flash("Petition rejected.");
            } else {
                $pdo->beginTransaction();

                // Drop subject if request was drop or change
                if (in_array($req['request_type'], ['drop', 'change'], true) && $req['class_offering_id']) {
                    $upd = $pdo->prepare("UPDATE enrolled_subjects SET status = 'dropped' WHERE enrollment_id = ? AND class_offering_id = ? AND status = 'enrolled'");
                    $upd->execute([$req['enrollment_id'], $req['class_offering_id']]);
                    $pdo->prepare("UPDATE class_offerings SET slots_taken = GREATEST(slots_taken - 1, 0) WHERE id = ?")->execute([$req['class_offering_id']]);
                }

                // Add subject if request was add or change
                if (in_array($req['request_type'], ['add', 'change'], true) && $req['target_class_offering_id']) {
                    $ins = $pdo->prepare("INSERT INTO enrolled_subjects (enrollment_id, class_offering_id, status) VALUES (?, ?, 'enrolled')");
                    $ins->execute([$req['enrollment_id'], $req['target_class_offering_id']]);
                    $pdo->prepare("UPDATE class_offerings SET slots_taken = slots_taken + 1 WHERE id = ?")->execute([$req['target_class_offering_id']]);
                }

                $upd = $pdo->prepare("UPDATE add_drop_requests SET status = 'approved', processed_at = NOW(), processed_by = ? WHERE id = ?");
                $upd->execute([$registrarId, $reqId]);

                $pdo->commit();
                log_activity($pdo, $req['student_id'], ucfirst($req['request_type']) . " subject petition approved by registrar.");
                set_flash("Petition approved and class schedule updated.");
            }
        }

        // 3. Overload / Waiver Approval
        elseif ($action === 'approve_overload' || $action === 'reject_overload') {
            $reqId = (int) ($_POST['request_id'] ?? 0);
            $newStatus = ($action === 'approve_overload') ? 'approved' : 'rejected';

            $stmt = $pdo->prepare("SELECT student_id, request_type, requested_units FROM overload_waiver_requests WHERE id = ? AND status = 'pending'");
            $stmt->execute([$reqId]);
            $req = $stmt->fetch();

            if ($req) {
                $upd = $pdo->prepare("UPDATE overload_waiver_requests SET status = ?, processed_at = NOW(), processed_by = ? WHERE id = ?");
                $upd->execute([$newStatus, $registrarId, $reqId]);
                log_activity($pdo, $req['student_id'], ucfirst($req['request_type']) . " petition {$newStatus} by registrar.");
                set_flash(ucfirst($req['request_type']) . " petition {$newStatus}.");
            } else {
                set_flash("That petition is no longer pending.", 'error');
            }
        }
    } catch (Throwable $e) {
        if ($pdo->inTransaction()) $pdo->rollBack();
        set_flash("Operation failed: " . $e->getMessage(), 'error');
    }

    header("Location: enrollment_validation.php?tab={$tab}");
    exit;
}

// Queries for View
// 1. Pending Enrollments
$pendingEnrollments = $pdo->prepare(
    "SELECT e.id, e.school_year, e.semester, e.enrollment_type, e.total_units, e.enrolled_at,
            u.name, u.student_id_number, u.email, sp.program, sp.year_level, sp.academic_status,
            (SELECT COUNT(*) FROM enrolled_subjects es WHERE es.enrollment_id = e.id) as subjects_count
     FROM enrollments e
     JOIN `user` u ON u.id = e.student_id
     LEFT JOIN student_profile sp ON sp.user_id = u.id
     WHERE e.status = 'pending'
     ORDER BY e.enrolled_at ASC"
);
$pendingEnrollments->execute();
$pendingList = $pendingEnrollments->fetchAll();

// 2. Add / Drop Petitions
$pendingAddDrop = $pdo->query(
    "SELECT adr.id, adr.request_type, adr.reason, adr.requested_at,
            u.name, u.student_id_number, sp.program,
            s_from.subject_code AS from_code, s_from.subject_name AS from_name,
            s_to.subject_code AS to_code, s_to.subject_name AS to_name
     FROM add_drop_requests adr
     JOIN `user` u ON u.id = adr.student_id
     LEFT JOIN student_profile sp ON sp.user_id = u.id
     LEFT JOIN class_offerings co_from ON co_from.id = adr.class_offering_id
     LEFT JOIN subjects s_from ON s_from.id = co_from.subject_id
     LEFT JOIN class_offerings co_to ON co_to.id = adr.target_class_offering_id
     LEFT JOIN subjects s_to ON s_to.id = co_to.subject_id
     WHERE adr.status = 'pending'
     ORDER BY adr.requested_at ASC"
)->fetchAll();

// 3. Overload / Waiver Petitions
$pendingOverloads = $pdo->query(
    "SELECT owr.id, owr.request_type, owr.requested_units, owr.reason, owr.requested_at,
            u.name, u.student_id_number, sp.program, sp.year_level, sp.average_grade
     FROM overload_waiver_requests owr
     JOIN `user` u ON u.id = owr.student_id
     LEFT JOIN student_profile sp ON sp.user_id = u.id
     WHERE owr.status = 'pending'
     ORDER BY owr.requested_at ASC"
)->fetchAll();

// 4. Active Validated Enrollments
$activeEnrollments = $pdo->prepare(
    "SELECT e.id, e.school_year, e.semester, e.enrollment_type, e.total_units, e.enrolled_at,
            u.id as student_id, u.name, u.student_id_number, sp.program, sp.year_level
     FROM enrollments e
     JOIN `user` u ON u.id = e.student_id
     LEFT JOIN student_profile sp ON sp.user_id = u.id
     WHERE e.status = 'active' AND e.school_year = ? AND e.semester = ?
     ORDER BY u.name ASC"
);
$activeEnrollments->execute([$currentSchoolYear, $currentSemester]);
$activeList = $activeEnrollments->fetchAll();
?>
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Enrollment &amp; Validation Management - Registrar</title>
  <link rel="stylesheet" href="dashboard.css">
</head>
<body class="dashboard-body">

  <?php render_sidebar('enrollment_validation.php'); ?>

  <div class="dash-main">
    <?php render_topbar($pdo, 'Enrollment & Validation'); ?>

    <div class="dash-content">
      <?php if ($flash): ?>
        <div class="alert-banner alert-<?php echo htmlspecialchars($flash['type']); ?>">
          <span><?php echo htmlspecialchars($flash['message']); ?></span>
        </div>
      <?php endif; ?>

      <div class="dash-heading-row">
        <div>
          <h1 class="dash-heading">📝 Enrollment &amp; Validation Engine</h1>
          <p class="dash-subheading">Review matriculation applications, process add/drop changes, and approve unit overloads.</p>
        </div>
      </div>

      <!-- Tab Navigation -->
      <div class="tab-nav">
        <a href="enrollment_validation.php?tab=registration" class="tab-link <?php echo $tab === 'registration' ? 'active' : ''; ?>">
          <span>⏳ Pending Enrollments (<?php echo count($pendingList); ?>)</span>
        </a>
        <a href="enrollment_validation.php?tab=adddrop" class="tab-link <?php echo $tab === 'adddrop' ? 'active' : ''; ?>">
          <span>🔄 Add / Drop Petitions (<?php echo count($pendingAddDrop); ?>)</span>
        </a>
        <a href="enrollment_validation.php?tab=overload" class="tab-link <?php echo $tab === 'overload' ? 'active' : ''; ?>">
          <span>⚡ Overload &amp; Waivers (<?php echo count($pendingOverloads); ?>)</span>
        </a>
        <a href="enrollment_validation.php?tab=active_list" class="tab-link <?php echo $tab === 'active_list' ? 'active' : ''; ?>">
          <span>✅ Officially Enrolled Roster (<?php echo count($activeList); ?>)</span>
        </a>
      </div>

      <!-- TAB 1: PENDING REGISTRATION APPROVAL -->
      <?php if ($tab === 'registration'): ?>
        <div class="dash-panel">
          <div class="dash-panel-header">
            <h2>Pending Registration Assessment Queue</h2>
          </div>
          <?php if (!$pendingList): ?>
            <p style="text-align:center;padding:30px;color:#64748b;">🎉 All student registration assessments have been processed!</p>
          <?php else: ?>
            <div class="table-responsive">
              <table class="table-simple">
                <thead>
                  <tr>
                    <th>Student Details</th>
                    <th>Program &amp; Year</th>
                    <th>Type</th>
                    <th>Enrolled Load</th>
                    <th>Academic Status</th>
                    <th>Submitted</th>
                    <th style="text-align:right;">Validation Action</th>
                  </tr>
                </thead>
                <tbody>
                  <?php foreach ($pendingList as $enr): ?>
                  <tr>
                    <td>
                      <strong><?php echo htmlspecialchars($enr['name']); ?></strong>
                      <div style="font-size:11px;color:#64748b;"><?php echo htmlspecialchars($enr['student_id_number'] ?: 'NO-ID'); ?> &middot; <?php echo htmlspecialchars($enr['email']); ?></div>
                    </td>
                    <td>
                      <div><?php echo htmlspecialchars($enr['program']); ?></div>
                      <div style="font-size:11px;color:#64748b;"><?php echo htmlspecialchars($enr['year_level']); ?></div>
                    </td>
                    <td><span class="badge badge-open"><?php echo htmlspecialchars(ucfirst($enr['enrollment_type'])); ?></span></td>
                    <td>
                      <strong><?php echo htmlspecialchars($enr['total_units']); ?> units</strong>
                      <div style="font-size:11px;color:#64748b;"><?php echo $enr['subjects_count']; ?> subjects</div>
                    </td>
                    <td><span class="badge badge-<?php echo strtolower(str_replace(' ', '_', $enr['academic_status'])); ?>"><?php echo htmlspecialchars($enr['academic_status']); ?></span></td>
                    <td><?php echo date('M d, Y h:i A', strtotime($enr['enrolled_at'])); ?></td>
                    <td style="text-align:right;">
                      <form method="post" style="display:inline-flex;gap:6px;">
                        <input type="hidden" name="enrollment_id" value="<?php echo $enr['id']; ?>">
                        <button type="submit" name="action" value="approve_enrollment" class="btn-small" style="background:var(--success);color:#fff;border-color:var(--success);">Approve &amp; Validate</button>
                        <button type="submit" name="action" value="reject_enrollment" class="btn-small danger" onclick="return confirm('Reject this student enrollment application?')">Reject</button>
                      </form>
                    </td>
                  </tr>
                  <?php endforeach; ?>
                </tbody>
              </table>
            </div>
          <?php endif; ?>
        </div>

      <!-- TAB 2: ADD / DROP PETITIONS -->
      <?php elseif ($tab === 'adddrop'): ?>
        <div class="dash-panel">
          <div class="dash-panel-header">
            <h2>Add / Drop / Change Subject Petitions</h2>
          </div>
          <?php if (!$pendingAddDrop): ?>
            <p style="text-align:center;padding:30px;color:#64748b;">No pending Add/Drop petitions at this time.</p>
          <?php else: ?>
            <div class="table-responsive">
              <table class="table-simple">
                <thead>
                  <tr>
                    <th>Student</th>
                    <th>Type</th>
                    <th>Subject to Drop</th>
                    <th>Subject to Add</th>
                    <th>Reason</th>
                    <th>Action</th>
                  </tr>
                </thead>
                <tbody>
                  <?php foreach ($pendingAddDrop as $ad): ?>
                  <tr>
                    <td>
                      <strong><?php echo htmlspecialchars($ad['name']); ?></strong>
                      <div style="font-size:11px;color:#64748b;"><?php echo htmlspecialchars($ad['student_id_number']); ?></div>
                    </td>
                    <td><span class="badge badge-pending"><?php echo htmlspecialchars(strtoupper($ad['request_type'])); ?></span></td>
                    <td><?php echo $ad['from_code'] ? "<strong>{$ad['from_code']}</strong> - {$ad['from_name']}" : '—'; ?></td>
                    <td><?php echo $ad['to_code'] ? "<strong style='color:var(--accent);'>{$ad['to_code']}</strong> - {$ad['to_name']}" : '—'; ?></td>
                    <td style="font-size:12px;max-width:240px;color:#475569;"><?php echo htmlspecialchars($ad['reason']); ?></td>
                    <td>
                      <form method="post" style="display:flex;gap:6px;">
                        <input type="hidden" name="request_id" value="<?php echo $ad['id']; ?>">
                        <button type="submit" name="action" value="approve_adddrop" class="btn-small primary">Approve</button>
                        <button type="submit" name="action" value="reject_adddrop" class="btn-small danger">Reject</button>
                      </form>
                    </td>
                  </tr>
                  <?php endforeach; ?>
                </tbody>
              </table>
            </div>
          <?php endif; ?>
        </div>

      <!-- TAB 3: OVERLOAD / WAIVER REQUESTS -->
      <?php elseif ($tab === 'overload'): ?>
        <div class="dash-panel">
          <div class="dash-panel-header">
            <h2>Unit Overload &amp; Prerequisite Waiver Petitions</h2>
          </div>
          <?php if (!$pendingOverloads): ?>
            <p style="text-align:center;padding:30px;color:#64748b;">No pending overload or waiver petitions.</p>
          <?php else: ?>
            <div class="table-responsive">
              <table class="table-simple">
                <thead>
                  <tr>
                    <th>Student Details</th>
                    <th>Petition Type</th>
                    <th>Requested Units</th>
                    <th>GWA</th>
                    <th>Justification</th>
                    <th>Action</th>
                  </tr>
                </thead>
                <tbody>
                  <?php foreach ($pendingOverloads as $ov): ?>
                  <tr>
                    <td>
                      <strong><?php echo htmlspecialchars($ov['name']); ?></strong>
                      <div style="font-size:11px;color:#64748b;"><?php echo htmlspecialchars($ov['program']); ?> &middot; <?php echo htmlspecialchars($ov['year_level']); ?></div>
                    </td>
                    <td><span class="badge badge-open"><?php echo htmlspecialchars(strtoupper($ov['request_type'])); ?></span></td>
                    <td><strong><?php echo htmlspecialchars($ov['requested_units']); ?> units</strong></td>
                    <td><?php echo $ov['average_grade'] ? number_format((float)$ov['average_grade'], 2) : 'N/A'; ?></td>
                    <td style="font-size:12px;max-width:280px;color:#475569;"><?php echo htmlspecialchars($ov['reason']); ?></td>
                    <td>
                      <form method="post" style="display:flex;gap:6px;">
                        <input type="hidden" name="request_id" value="<?php echo $ov['id']; ?>">
                        <button type="submit" name="action" value="approve_overload" class="btn-small primary">Approve</button>
                        <button type="submit" name="action" value="reject_overload" class="btn-small danger">Reject</button>
                      </form>
                    </td>
                  </tr>
                  <?php endforeach; ?>
                </tbody>
              </table>
            </div>
          <?php endif; ?>
        </div>

      <!-- TAB 4: ACTIVE VALIDATED ENROLLEES -->
      <?php elseif ($tab === 'active_list'): ?>
        <div class="dash-panel">
          <div class="dash-panel-header">
            <h2>Officially Validated Students (AY <?php echo htmlspecialchars($currentSchoolYear); ?> <?php echo htmlspecialchars($currentSemester); ?>)</h2>
          </div>
          <div class="table-responsive">
            <table class="table-simple">
              <thead>
                <tr>
                  <th>Student ID</th>
                  <th>Full Name</th>
                  <th>Program</th>
                  <th>Year Level</th>
                  <th>Total Units</th>
                  <th>Matriculation Type</th>
                  <th>Validated At</th>
                  <th class="no-print">Certificate of Matriculation</th>
                </tr>
              </thead>
              <tbody>
                <?php foreach ($activeList as $row): ?>
                <tr>
                  <td><strong style="color:var(--accent);"><?php echo htmlspecialchars($row['student_id_number']); ?></strong></td>
                  <td><strong><?php echo htmlspecialchars($row['name']); ?></strong></td>
                  <td><?php echo htmlspecialchars($row['program']); ?></td>
                  <td><?php echo htmlspecialchars($row['year_level']); ?></td>
                  <td><?php echo htmlspecialchars($row['total_units']); ?> units</td>
                  <td><span class="badge badge-open"><?php echo htmlspecialchars(ucfirst($row['enrollment_type'])); ?></span></td>
                  <td><?php echo date('M d, Y', strtotime($row['enrolled_at'])); ?></td>
                  <td class="no-print">
                    <a href="student_records.php?id=<?php echo $row['student_id']; ?>" class="btn-small">View Record &amp; COR ›</a>
                  </td>
                </tr>
                <?php endforeach; ?>
              </tbody>
            </table>
          </div>
        </div>
      <?php endif; ?>

    </div>
  </div>

</body>
</html>