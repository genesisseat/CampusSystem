<?php
require 'config.php';
$displayName = $_SESSION['user_name'] ?? 'Registrar';

/**
 * Reads/writes `subjects` and `class_offerings`, both created in
 * enrollment_validation_schema.sql. Run class_scheduling_schema.sql too
 * (adds days_of_week / start_time / end_time to class_offerings) before
 * using this page.
 */

$currentSchoolYear = $pdo->query("SELECT `value` FROM settings WHERE `key` = 'current_school_year'")->fetchColumn() ?: '';
$currentSemester   = $pdo->query("SELECT `value` FROM settings WHERE `key` = 'current_semester'")->fetchColumn() ?: '';

$allDays = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

$flash = null;
$flashType = 'ok';

if (isset($_SESSION['cs_flash'])) {
    $flash = $_SESSION['cs_flash'];
    $flashType = $_SESSION['cs_flash_type'] ?? 'ok';
    unset($_SESSION['cs_flash'], $_SESSION['cs_flash_type']);
}

if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $action = $_POST['action'] ?? '';

    try {
        // ---- Add a subject to the catalog ----
        if ($action === 'add_subject') {
            $code = trim($_POST['subject_code'] ?? '');
            $name = trim($_POST['subject_name'] ?? '');
            $units = (float) ($_POST['units'] ?? 3);
            $prereq = $_POST['prerequisite_subject_id'] !== '' ? (int) $_POST['prerequisite_subject_id'] : null;

            if ($code === '' || $name === '') {
                $flash = 'Subject code and name are required.';
                $flashType = 'error';
            } else {
                $stmt = $pdo->prepare(
                    "INSERT INTO subjects (subject_code, subject_name, units, prerequisite_subject_id) VALUES (?, ?, ?, ?)"
                );
                $stmt->execute([$code, $name, $units, $prereq]);
                $flash = "Subject {$code} added.";
            }
        }

        // ---- Delete a subject (only if no offerings reference it) ----
        elseif ($action === 'delete_subject') {
            $id = (int) ($_POST['subject_id'] ?? 0);
            $inUse = $pdo->prepare("SELECT COUNT(*) FROM class_offerings WHERE subject_id = ?");
            $inUse->execute([$id]);
            if ($inUse->fetchColumn() > 0) {
                $flash = 'Cannot delete: this subject has existing class offerings.';
                $flashType = 'error';
            } else {
                $pdo->prepare("DELETE FROM subjects WHERE id = ?")->execute([$id]);
                $flash = 'Subject deleted.';
            }
        }

        // ---- Create a class offering, with conflict check ----
        elseif ($action === 'add_offering') {
            $subjectId = (int) ($_POST['subject_id'] ?? 0);
            $sectionCode = trim($_POST['section_code'] ?? '');
            $room = trim($_POST['room'] ?? '');
            $capacity = (int) ($_POST['capacity'] ?? 40);
            $days = $_POST['days'] ?? [];
            $days = array_values(array_intersect($days, $allDays)); // sanitize to known values
            $startTime = $_POST['start_time'] ?? '';
            $endTime = $_POST['end_time'] ?? '';

            if (!$subjectId || $sectionCode === '' || $room === '' || !$days || !$startTime || !$endTime) {
                $flash = 'Please fill in subject, section, room, days, and time.';
                $flashType = 'error';
            } elseif ($startTime >= $endTime) {
                $flash = 'Start time must be before end time.';
                $flashType = 'error';
            } else {
                // Conflict check: same room, same term, overlapping days, overlapping time
                $stmt = $pdo->prepare(
                    "SELECT co.section_code, co.days_of_week, co.start_time, co.end_time, s.subject_code
                     FROM class_offerings co
                     JOIN subjects s ON s.id = co.subject_id
                     WHERE co.room = ? AND co.school_year = ? AND co.semester = ?
                       AND co.start_time < ? AND co.end_time > ?"
                );
                $stmt->execute([$room, $currentSchoolYear, $currentSemester, $endTime, $startTime]);
                $candidates = $stmt->fetchAll();

                $conflict = null;
                foreach ($candidates as $c) {
                    $existingDays = array_map('trim', explode(',', (string) $c['days_of_week']));
                    if (array_intersect($days, $existingDays)) {
                        $conflict = $c;
                        break;
                    }
                }

                if ($conflict) {
                    $flash = "Room conflict: {$conflict['subject_code']} ({$conflict['section_code']}) already uses {$room} on overlapping days/time ({$conflict['start_time']}–{$conflict['end_time']}).";
                    $flashType = 'error';
                } else {
                    $stmt = $pdo->prepare(
                        "INSERT INTO class_offerings
                         (subject_id, section_code, school_year, semester, room, days_of_week, start_time, end_time, capacity, slots_taken, status)
                         VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, 0, 'open')"
                    );
                    $stmt->execute([
                        $subjectId, $sectionCode, $currentSchoolYear, $currentSemester,
                        $room, implode(',', $days), $startTime, $endTime, $capacity,
                    ]);
                    $flash = "Class offering {$sectionCode} created.";
                }
            }
        }

        // ---- Close/reopen or delete an offering ----
        elseif ($action === 'toggle_offering') {
            $id = (int) ($_POST['offering_id'] ?? 0);
            $stmt = $pdo->prepare("SELECT status FROM class_offerings WHERE id = ?");
            $stmt->execute([$id]);
            $current = $stmt->fetchColumn();
            if ($current !== false) {
                $newStatus = $current === 'open' ? 'closed' : 'open';
                $pdo->prepare("UPDATE class_offerings SET status = ? WHERE id = ?")->execute([$newStatus, $id]);
                $flash = "Offering marked {$newStatus}.";
            }
        } elseif ($action === 'delete_offering') {
            $id = (int) ($_POST['offering_id'] ?? 0);
            $inUse = $pdo->prepare("SELECT COUNT(*) FROM enrolled_subjects WHERE class_offering_id = ? AND status = 'enrolled'");
            $inUse->execute([$id]);
            if ($inUse->fetchColumn() > 0) {
                $flash = 'Cannot delete: students are currently enrolled in this offering.';
                $flashType = 'error';
            } else {
                $pdo->prepare("DELETE FROM class_offerings WHERE id = ?")->execute([$id]);
                $flash = 'Offering deleted.';
            }
        }
    } catch (Throwable $e) {
        $flash = 'Something went wrong: ' . $e->getMessage();
        $flashType = 'error';
    }

    $_SESSION['cs_flash'] = $flash;
    $_SESSION['cs_flash_type'] = $flashType;
    header('Location: class_scheduling.php');
    exit;
}

$subjects = $pdo->query("SELECT id, subject_code, subject_name, units FROM subjects ORDER BY subject_code")->fetchAll();

$offerings = $pdo->prepare(
    "SELECT co.id, co.section_code, co.room, co.days_of_week, co.start_time, co.end_time,
            co.capacity, co.slots_taken, co.status, s.subject_code, s.subject_name
     FROM class_offerings co
     JOIN subjects s ON s.id = co.subject_id
     WHERE co.school_year = ? AND co.semester = ?
     ORDER BY s.subject_code, co.section_code"
);
$offerings->execute([$currentSchoolYear, $currentSemester]);
$offerings = $offerings->fetchAll();
?>
<!doctype html>
<html>
<head>
<meta charset="utf-8">
<title>Class Scheduling & Room Allocation - Registrar</title>
<link rel="stylesheet" href="styles.css">
<link rel="stylesheet" href="dashboard.css">
<style>
  .cs-form { display: grid; grid-template-columns: repeat(4, 1fr); gap: 12px; margin-bottom: 16px; }
  .cs-form label { font-size: 12px; color: #5c6b7f; font-weight: 600; margin-top: 0; }
  .cs-form input, .cs-form select {
    width: 100%; padding: 9px 10px; border-radius: 8px; border: 1px solid #d7dee8; font-size: 13px; margin-top: 4px;
  }
  .cs-days { display: flex; gap: 10px; flex-wrap: wrap; margin-top: 4px; }
  .cs-days label { display: flex; align-items: center; gap: 4px; font-weight: 500; color: #33475b; }
  .cs-span-4 { grid-column: span 4; }
  .cs-span-2 { grid-column: span 2; }
  form.inline { display: inline; }
  .btn-reject { background: #d64545; }
  .btn-small.secondary { margin-left: 6px; }
</style>
</head>
<body class="dashboard-body">

  <aside class="dash-sidebar">
    <div class="dash-brand"><span class="cap">🎓</span> Registrar System</div>
    <nav class="dash-nav">
      <a href="admin_dashboard.php"><span class="ic">🏠</span> Dashboard</a>
      <a href="student_records.php"><span class="ic">🗂️</span> Student Records (201 File)</a>
      <a href="enrollment_validation.php"><span class="ic">📝</span> Enrollment &amp; Validation</a>
      <a href="class_scheduling.php" class="active"><span class="ic">🏫</span> Class Scheduling</a>
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
      <h1 class="dash-heading">🏫 Master Class Scheduling &amp; Room Allocation</h1>
      <p class="dash-subheading">Set up class offerings, assign rooms and schedules for <?php echo htmlspecialchars($currentSemester . ' ' . $currentSchoolYear); ?>, and prevent scheduling conflicts.</p>

      <?php if ($flash): ?>
        <div class="flash-note" style="<?php echo $flashType === 'error' ? 'background:#fdeaea;color:#d64545;border-color:rgba(214,69,69,0.25);' : ''; ?>">
          <?php echo htmlspecialchars($flash); ?>
        </div>
      <?php endif; ?>

      <!-- Manage Subjects -->
      <div class="dash-panel" style="margin-bottom:18px;">
        <h2>Subject Catalog</h2>
        <form method="post" class="cs-form">
          <input type="hidden" name="action" value="add_subject">
          <div>
            <label>Subject Code</label>
            <input type="text" name="subject_code" placeholder="e.g. IT101" required>
          </div>
          <div class="cs-span-2">
            <label>Subject Name</label>
            <input type="text" name="subject_name" placeholder="e.g. Introduction to Computing" required>
          </div>
          <div>
            <label>Units</label>
            <input type="number" step="0.5" name="units" value="3" required>
          </div>
          <div class="cs-span-2">
            <label>Prerequisite (optional)</label>
            <select name="prerequisite_subject_id">
              <option value="">— None —</option>
              <?php foreach ($subjects as $s): ?>
                <option value="<?php echo (int) $s['id']; ?>"><?php echo htmlspecialchars($s['subject_code'] . ' — ' . $s['subject_name']); ?></option>
              <?php endforeach; ?>
            </select>
          </div>
          <div style="align-self:end;">
            <button class="btn-small" type="submit">Add Subject</button>
          </div>
        </form>

        <?php if (!$subjects): ?>
          <p class="empty-note">No subjects yet — add one above.</p>
        <?php else: ?>
          <table class="table-simple">
            <thead><tr><th>Code</th><th>Name</th><th>Units</th><th></th></tr></thead>
            <tbody>
              <?php foreach ($subjects as $s): ?>
              <tr>
                <td><?php echo htmlspecialchars($s['subject_code']); ?></td>
                <td><?php echo htmlspecialchars($s['subject_name']); ?></td>
                <td><?php echo htmlspecialchars($s['units']); ?></td>
                <td>
                  <form class="inline" method="post" onsubmit="return confirm('Delete this subject?');">
                    <input type="hidden" name="action" value="delete_subject">
                    <input type="hidden" name="subject_id" value="<?php echo (int) $s['id']; ?>">
                    <button class="btn-small btn-reject" type="submit">Delete</button>
                  </form>
                </td>
              </tr>
              <?php endforeach; ?>
            </tbody>
          </table>
        <?php endif; ?>
      </div>

      <!-- Create Class Offering -->
      <div class="dash-panel" style="margin-bottom:18px;">
        <h2>Create Class Offering</h2>
        <?php if (!$subjects): ?>
          <p class="empty-note">Add at least one subject above before creating an offering.</p>
        <?php else: ?>
        <form method="post" class="cs-form">
          <input type="hidden" name="action" value="add_offering">
          <div class="cs-span-2">
            <label>Subject</label>
            <select name="subject_id" required>
              <?php foreach ($subjects as $s): ?>
                <option value="<?php echo (int) $s['id']; ?>"><?php echo htmlspecialchars($s['subject_code'] . ' — ' . $s['subject_name']); ?></option>
              <?php endforeach; ?>
            </select>
          </div>
          <div>
            <label>Section Code</label>
            <input type="text" name="section_code" placeholder="e.g. A1" required>
          </div>
          <div>
            <label>Capacity</label>
            <input type="number" name="capacity" value="40" required>
          </div>
          <div class="cs-span-4">
            <label>Days</label>
            <div class="cs-days">
              <?php foreach ($allDays as $d): ?>
                <label><input type="checkbox" name="days[]" value="<?php echo $d; ?>"> <?php echo $d; ?></label>
              <?php endforeach; ?>
            </div>
          </div>
          <div>
            <label>Start Time</label>
            <input type="time" name="start_time" required>
          </div>
          <div>
            <label>End Time</label>
            <input type="time" name="end_time" required>
          </div>
          <div class="cs-span-2">
            <label>Room</label>
            <input type="text" name="room" placeholder="e.g. Room 301" required>
          </div>
          <div class="cs-span-4">
            <button class="btn-small" type="submit">Create Offering</button>
          </div>
        </form>
        <?php endif; ?>
      </div>

      <!-- Existing Offerings -->
      <div class="dash-panel">
        <h2>Offerings — <?php echo htmlspecialchars($currentSemester . ' ' . $currentSchoolYear); ?></h2>
        <?php if (!$offerings): ?>
          <p class="empty-note">No class offerings for this term yet.</p>
        <?php else: ?>
          <table class="table-simple">
            <thead><tr><th>Subject</th><th>Section</th><th>Room</th><th>Schedule</th><th>Slots</th><th>Status</th><th></th></tr></thead>
            <tbody>
              <?php foreach ($offerings as $o):
                $full = $o['slots_taken'] >= $o['capacity'];
              ?>
              <tr>
                <td><?php echo htmlspecialchars($o['subject_code'] . ' — ' . $o['subject_name']); ?></td>
                <td><?php echo htmlspecialchars($o['section_code']); ?></td>
                <td><?php echo htmlspecialchars($o['room']); ?></td>
                <td><?php echo htmlspecialchars($o['days_of_week'] . ' ' . substr($o['start_time'], 0, 5) . '–' . substr($o['end_time'], 0, 5)); ?></td>
                <td><?php echo (int) $o['slots_taken'] . ' / ' . (int) $o['capacity']; ?><?php if ($full) echo ' <span class="badge badge-dropped">Full</span>'; ?></td>
                <td><span class="badge <?php echo $o['status'] === 'open' ? 'badge-active' : 'badge-on-leave'; ?>"><?php echo htmlspecialchars(ucfirst($o['status'])); ?></span></td>
                <td>
                  <form class="inline" method="post">
                    <input type="hidden" name="action" value="toggle_offering">
                    <input type="hidden" name="offering_id" value="<?php echo (int) $o['id']; ?>">
                    <button class="btn-small secondary" type="submit"><?php echo $o['status'] === 'open' ? 'Close' : 'Reopen'; ?></button>
                  </form>
                  <form class="inline" method="post" onsubmit="return confirm('Delete this offering?');">
                    <input type="hidden" name="action" value="delete_offering">
                    <input type="hidden" name="offering_id" value="<?php echo (int) $o['id']; ?>">
                    <button class="btn-small btn-reject" type="submit">Delete</button>
                  </form>
                </td>
              </tr>
              <?php endforeach; ?>
            </tbody>
          </table>
        <?php endif; ?>
      </div>
    </div>
  </div>

</body>
</html>