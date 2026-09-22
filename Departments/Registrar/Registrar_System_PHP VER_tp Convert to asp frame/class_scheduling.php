<?php
require 'config.php';

$currentSchoolYear = get_setting($pdo, 'current_school_year', '2025-2026');
$currentSemester   = get_setting($pdo, 'current_semester', '1st Semester');
$displayName       = $_SESSION['user_name'] ?? 'Registrar';

$allDays = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
$tab = $_GET['tab'] ?? 'offerings';
if (!in_array($tab, ['offerings', 'subjects', 'roster'], true)) {
    $tab = 'offerings';
}

$flash = get_flash();

// Handle Form Submissions
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $action = $_POST['action'] ?? '';

    try {
        // 1. Add Subject to Catalog
        if ($action === 'add_subject') {
            $code = strtoupper(trim($_POST['subject_code'] ?? ''));
            $name = trim($_POST['subject_name'] ?? '');
            $units = (float) ($_POST['units'] ?? 3.0);
            $lec = (int) ($_POST['lec_hours'] ?? 3);
            $lab = (int) ($_POST['lab_hours'] ?? 0);
            $prereq = !empty($_POST['prerequisite_subject_id']) ? (int)$_POST['prerequisite_subject_id'] : null;
            $prog = trim($_POST['curriculum_program'] ?? 'BSIT');
            $yr = trim($_POST['year_level'] ?? '1st Year');
            $sem = trim($_POST['semester'] ?? '1st Semester');

            if ($code === '' || $name === '') {
                set_flash("Subject code and descriptive title are required.", 'error');
            } else {
                $stmt = $pdo->prepare("INSERT INTO subjects (subject_code, subject_name, units, lec_hours, lab_hours, prerequisite_subject_id, curriculum_program, year_level, semester) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)");
                $stmt->execute([$code, $name, $units, $lec, $lab, $prereq, $prog, $yr, $sem]);
                log_activity($pdo, $_SESSION['user_id'] ?? null, "Subject {$code} ({$name}) added to catalog by {$displayName}.");
                set_flash("Subject {$code} added successfully.");
            }
            header("Location: class_scheduling.php?tab=subjects");
            exit;
        }

        // 2. Delete Subject
        if ($action === 'delete_subject') {
            $id = (int) ($_POST['subject_id'] ?? 0);
            $inUse = $pdo->prepare("SELECT COUNT(*) FROM class_offerings WHERE subject_id = ?");
            $inUse->execute([$id]);
            if ($inUse->fetchColumn() > 0) {
                set_flash("Cannot delete subject: class offerings currently reference this course.", 'error');
            } else {
                $pdo->prepare("DELETE FROM subjects WHERE id = ?")->execute([$id]);
                set_flash("Subject deleted from catalog.");
            }
            header("Location: class_scheduling.php?tab=subjects");
            exit;
        }

        // 3. Create Class Offering with Room/Time Conflict Detection
        if ($action === 'add_offering') {
            $subjectId   = (int) ($_POST['subject_id'] ?? 0);
            $sectionCode = strtoupper(trim($_POST['section_code'] ?? ''));
            $room        = strtoupper(trim($_POST['room'] ?? ''));
            $capacity    = (int) ($_POST['capacity'] ?? 40);
            $instructor  = trim($_POST['instructor_name'] ?? 'Prof. TBD');
            $days        = $_POST['days'] ?? [];
            $days        = array_values(array_intersect($days, $allDays));
            $startTime   = $_POST['start_time'] ?? '';
            $endTime     = $_POST['end_time'] ?? '';

            if (!$subjectId || $sectionCode === '' || $room === '' || empty($days) || !$startTime || !$endTime) {
                set_flash("Please complete all schedule details (subject, section, room, days, and time).", 'error');
            } elseif ($startTime >= $endTime) {
                set_flash("Start time must precede end time.", 'error');
            } else {
                // Conflict check: overlapping room, same AY/semester, overlapping time and days
                $stmt = $pdo->prepare(
                    "SELECT co.section_code, co.days_of_week, co.start_time, co.end_time, s.subject_code, co.room
                     FROM class_offerings co
                     JOIN subjects s ON s.id = co.subject_id
                     WHERE co.room = ? AND co.school_year = ? AND co.semester = ? AND co.status != 'cancelled'
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
                    set_flash("Schedule Conflict: Room {$room} is already reserved by {$conflict['subject_code']} ({$conflict['section_code']}) on {$conflict['days_of_week']} {$conflict['start_time']}–{$conflict['end_time']}.", 'error');
                } else {
                    $stmt = $pdo->prepare(
                        "INSERT INTO class_offerings 
                         (subject_id, section_code, school_year, semester, room, days_of_week, start_time, end_time, instructor_name, capacity, slots_taken, status)
                         VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, 0, 'open')"
                    );
                    $stmt->execute([
                        $subjectId, $sectionCode, $currentSchoolYear, $currentSemester,
                        $room, implode(',', $days), $startTime, $endTime, $instructor, $capacity
                    ]);
                    log_activity($pdo, $_SESSION['user_id'] ?? null, "Class offering {$sectionCode} created for {$currentSchoolYear} by {$displayName}.");
                    set_flash("Class offering {$sectionCode} successfully scheduled.");
                }
            }
            header("Location: class_scheduling.php?tab=offerings");
            exit;
        }

        // 4. Toggle Status / Delete Offering
        if ($action === 'toggle_offering') {
            $id = (int) ($_POST['offering_id'] ?? 0);
            $stmt = $pdo->prepare("SELECT status FROM class_offerings WHERE id = ?");
            $stmt->execute([$id]);
            $cur = $stmt->fetchColumn();
            if ($cur) {
                $newStatus = ($cur === 'open') ? 'closed' : 'open';
                $pdo->prepare("UPDATE class_offerings SET status = ? WHERE id = ?")->execute([$newStatus, $id]);
                set_flash("Class offering marked {$newStatus}.");
            }
            header("Location: class_scheduling.php?tab=offerings");
            exit;
        }

        if ($action === 'delete_offering') {
            $id = (int) ($_POST['offering_id'] ?? 0);
            $inUse = $pdo->prepare("SELECT COUNT(*) FROM enrolled_subjects WHERE class_offering_id = ? AND status = 'enrolled'");
            $inUse->execute([$id]);
            if ($inUse->fetchColumn() > 0) {
                set_flash("Cannot delete class offering: students are currently actively enrolled.", 'error');
            } else {
                $pdo->prepare("DELETE FROM class_offerings WHERE id = ?")->execute([$id]);
                set_flash("Class offering deleted.");
            }
            header("Location: class_scheduling.php?tab=offerings");
            exit;
        }
    } catch (Throwable $e) {
        set_flash("Error: " . $e->getMessage(), 'error');
        header("Location: class_scheduling.php?tab={$tab}");
        exit;
    }
}

// Queries
$subjectsList = $pdo->query("SELECT s.*, p.subject_code as prereq_code FROM subjects s LEFT JOIN subjects p ON p.id = s.prerequisite_subject_id ORDER BY s.subject_code ASC")->fetchAll();

$offeringsList = $pdo->query(
    "SELECT co.*, s.subject_code, s.subject_name, s.units
     FROM class_offerings co
     JOIN subjects s ON s.id = co.subject_id
     ORDER BY co.section_code ASC, s.subject_code ASC"
)->fetchAll();

// Roster Query
$selectedOfferingId = isset($_GET['offering_id']) ? (int)$_GET['offering_id'] : ($offeringsList[0]['id'] ?? 0);
$rosterStudents = [];
$selectedOfferingDetails = null;

if ($selectedOfferingId > 0) {
    $stmt = $pdo->prepare("SELECT co.*, s.subject_code, s.subject_name, s.units FROM class_offerings co JOIN subjects s ON s.id = co.subject_id WHERE co.id = ?");
    $stmt->execute([$selectedOfferingId]);
    $selectedOfferingDetails = $stmt->fetch();

    $stmtRoster = $pdo->prepare(
        "SELECT es.id as enrolled_sub_id, es.status as subject_status, u.id as student_id, u.name, u.student_id_number, u.email, sp.program, sp.year_level, g.grade, g.is_inc
         FROM enrolled_subjects es
         JOIN enrollments e ON e.id = es.enrollment_id
         JOIN `user` u ON u.id = e.student_id
         LEFT JOIN student_profile sp ON sp.user_id = u.id
         LEFT JOIN grades g ON g.enrolled_subject_id = es.id
         WHERE es.class_offering_id = ? AND es.status = 'enrolled'
         ORDER BY u.name ASC"
    );
    $stmtRoster->execute([$selectedOfferingId]);
    $rosterStudents = $stmtRoster->fetchAll();
}
?>
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Class Scheduling &amp; Course Management - Registrar</title>
  <link rel="stylesheet" href="dashboard.css">
</head>
<body class="dashboard-body">

  <?php render_sidebar('class_scheduling.php'); ?>

  <div class="dash-main">
    <?php render_topbar($pdo, 'Class Scheduling'); ?>

    <div class="dash-content">
      <?php if ($flash): ?>
        <div class="alert-banner alert-<?php echo htmlspecialchars($flash['type']); ?>">
          <span><?php echo htmlspecialchars($flash['message']); ?></span>
        </div>
      <?php endif; ?>

      <div class="dash-heading-row">
        <div>
          <h1 class="dash-heading">🏫 Class Scheduling &amp; Curriculum Catalog</h1>
          <p class="dash-subheading">Manage curriculum subjects, class offerings, room allocations, conflict checks, and class rosters.</p>
        </div>
        <div>
          <?php if ($tab === 'offerings'): ?>
            <button class="btn btn-primary" onclick="document.getElementById('addOfferingModal').style.display='block'">➕ Schedule New Class</button>
          <?php elseif ($tab === 'subjects'): ?>
            <button class="btn btn-primary" onclick="document.getElementById('addSubjectModal').style.display='block'">➕ Add Course Subject</button>
          <?php endif; ?>
        </div>
      </div>

      <!-- Tab Navigation -->
      <div class="tab-nav">
        <a href="class_scheduling.php?tab=offerings" class="tab-link <?php echo $tab === 'offerings' ? 'active' : ''; ?>">
          <span>📅 Class Schedule Offerings (<?php echo count($offeringsList); ?>)</span>
        </a>
        <a href="class_scheduling.php?tab=subjects" class="tab-link <?php echo $tab === 'subjects' ? 'active' : ''; ?>">
          <span>📚 Curriculum Subjects Catalog (<?php echo count($subjectsList); ?>)</span>
        </a>
        <a href="class_scheduling.php?tab=roster" class="tab-link <?php echo $tab === 'roster' ? 'active' : ''; ?>">
          <span>👥 Section Class Roster Viewer</span>
        </a>
      </div>

      <!-- TAB 1: CLASS OFFERINGS -->
      <?php if ($tab === 'offerings'): ?>
        <div class="dash-panel">
          <div class="dash-panel-header">
            <h2>Active Class Offerings for Term (AY <?php echo htmlspecialchars($currentSchoolYear); ?> <?php echo htmlspecialchars($currentSemester); ?>)</h2>
          </div>
          <div class="table-responsive">
            <table class="table-simple">
              <thead>
                <tr>
                  <th>Section</th>
                  <th>Course Code &amp; Title</th>
                  <th>Units</th>
                  <th>Room</th>
                  <th>Schedule Days &amp; Time</th>
                  <th>Assigned Faculty</th>
                  <th>Slots</th>
                  <th>Status</th>
                  <th style="text-align:right;">Control</th>
                </tr>
              </thead>
              <tbody>
                <?php foreach ($offeringsList as $co): 
                  $slotRatio = "{$co['slots_taken']}/{$co['capacity']}";
                  $isFull = ($co['slots_taken'] >= $co['capacity']);
                ?>
                <tr>
                  <td><strong style="color:var(--accent);"><?php echo htmlspecialchars($co['section_code']); ?></strong></td>
                  <td>
                    <strong><?php echo htmlspecialchars($co['subject_code']); ?></strong>
                    <div style="font-size:11px;color:#64748b;"><?php echo htmlspecialchars($co['subject_name']); ?></div>
                  </td>
                  <td><?php echo htmlspecialchars($co['units']); ?></td>
                  <td><span class="token-chip" style="font-weight:bold;"><?php echo htmlspecialchars($co['room']); ?></span></td>
                  <td>
                    <div style="font-weight:600;"><?php echo htmlspecialchars($co['days_of_week']); ?></div>
                    <div style="font-size:11px;color:#64748b;"><?php echo date('h:i A', strtotime($co['start_time'])); ?> – <?php echo date('h:i A', strtotime($co['end_time'])); ?></div>
                  </td>
                  <td><?php echo htmlspecialchars($co['instructor_name']); ?></td>
                  <td>
                    <span style="font-weight:700;color:<?php echo $isFull ? 'var(--danger)' : 'var(--text-main)'; ?>;">
                      <?php echo $slotRatio; ?>
                    </span>
                  </td>
                  <td><span class="badge badge-<?php echo strtolower($co['status']); ?>"><?php echo htmlspecialchars(ucfirst($co['status'])); ?></span></td>
                  <td style="text-align:right;">
                    <div style="display:inline-flex;gap:6px;">
                      <a href="class_scheduling.php?tab=roster&offering_id=<?php echo $co['id']; ?>" class="btn-small" title="View Students">👥 Roster</a>
                      <form method="post" style="display:inline;">
                        <input type="hidden" name="offering_id" value="<?php echo $co['id']; ?>">
                        <button type="submit" name="action" value="toggle_offering" class="btn-small"><?php echo ($co['status'] === 'open') ? 'Close' : 'Open'; ?></button>
                        <button type="submit" name="action" value="delete_offering" class="btn-small danger" onclick="return confirm('Delete this class offering?')">🗑️</button>
                      </form>
                    </div>
                  </td>
                </tr>
                <?php endforeach; ?>
              </tbody>
            </table>
          </div>
        </div>

      <!-- TAB 2: SUBJECTS CATALOG -->
      <?php elseif ($tab === 'subjects'): ?>
        <div class="dash-panel">
          <div class="dash-panel-header">
            <h2>Degree Curriculum Subjects Masterlist</h2>
          </div>
          <div class="table-responsive">
            <table class="table-simple">
              <thead>
                <tr>
                  <th>Course Code</th>
                  <th>Descriptive Title</th>
                  <th>Credit Units</th>
                  <th>Lec / Lab Hours</th>
                  <th>Prerequisite</th>
                  <th>Program &amp; Year</th>
                  <th style="text-align:right;">Action</th>
                </tr>
              </thead>
              <tbody>
                <?php foreach ($subjectsList as $s): ?>
                <tr>
                  <td><strong style="color:var(--accent);"><?php echo htmlspecialchars($s['subject_code']); ?></strong></td>
                  <td><strong><?php echo htmlspecialchars($s['subject_name']); ?></strong></td>
                  <td><?php echo htmlspecialchars($s['units']); ?>.0</td>
                  <td><?php echo (int)$s['lec_hours']; ?> hrs lec / <?php echo (int)$s['lab_hours']; ?> hrs lab</td>
                  <td>
                    <?php if ($s['prereq_code']): ?>
                      <span class="badge badge-inc"><?php echo htmlspecialchars($s['prereq_code']); ?></span>
                    <?php else: ?>
                      <span style="color:#94a3b8;">None</span>
                    <?php endif; ?>
                  </td>
                  <td>
                    <span><?php echo htmlspecialchars($s['curriculum_program']); ?></span>
                    <div style="font-size:11px;color:#64748b;"><?php echo htmlspecialchars($s['year_level'] . ', ' . $s['semester']); ?></div>
                  </td>
                  <td style="text-align:right;">
                    <form method="post" style="display:inline;">
                      <input type="hidden" name="subject_id" value="<?php echo $s['id']; ?>">
                      <button type="submit" name="action" value="delete_subject" class="btn-small danger" onclick="return confirm('Delete course subject?')">🗑️ Delete</button>
                    </form>
                  </td>
                </tr>
                <?php endforeach; ?>
              </tbody>
            </table>
          </div>
        </div>

      <!-- TAB 3: CLASS ROSTER -->
      <?php elseif ($tab === 'roster'): ?>
        <div class="dash-panel">
          <div class="dash-panel-header">
            <h2>Class Enrollee Roster Viewer</h2>
          </div>

          <form method="get" style="display:flex;gap:12px;align-items:center;margin-bottom:20px;">
            <input type="hidden" name="tab" value="roster">
            <label style="font-weight:700;font-size:13px;">Select Section &amp; Subject Offering:</label>
            <select name="offering_id" onchange="this.form.submit()" style="max-width:400px;">
              <?php foreach ($offeringsList as $o): ?>
                <option value="<?php echo $o['id']; ?>" <?php echo ($selectedOfferingId === $o['id']) ? 'selected' : ''; ?>>
                  <?php echo htmlspecialchars($o['section_code'] . ' — ' . $o['subject_code'] . ': ' . $o['subject_name'] . ' (' . $o['room'] . ')'); ?>
                </option>
              <?php endforeach; ?>
            </select>
          </form>

          <?php if ($selectedOfferingDetails): ?>
            <div class="stat-mini-row" style="margin-bottom:20px;">
              <div class="stat-mini">
                <div class="label">Section Code</div>
                <div class="value"><?php echo htmlspecialchars($selectedOfferingDetails['section_code']); ?></div>
              </div>
              <div class="stat-mini">
                <div class="label">Subject</div>
                <div class="value"><?php echo htmlspecialchars($selectedOfferingDetails['subject_code']); ?></div>
              </div>
              <div class="stat-mini">
                <div class="label">Room &amp; Time</div>
                <div class="value" style="font-size:13px;"><?php echo htmlspecialchars($selectedOfferingDetails['room'] . ' (' . $selectedOfferingDetails['days_of_week'] . ')'); ?></div>
              </div>
              <div class="stat-mini">
                <div class="label">Assigned Faculty</div>
                <div class="value" style="font-size:13px;"><?php echo htmlspecialchars($selectedOfferingDetails['instructor_name']); ?></div>
              </div>
              <div class="stat-mini">
                <div class="label">Total Enrolled</div>
                <div class="value" style="color:var(--accent);"><?php echo count($rosterStudents); ?> / <?php echo $selectedOfferingDetails['capacity']; ?></div>
              </div>
            </div>

            <?php if (empty($rosterStudents)): ?>
              <p style="text-align:center;padding:30px;color:#64748b;">No students currently enrolled in this section.</p>
            <?php else: ?>
              <div class="table-responsive">
                <table class="table-simple">
                  <thead>
                    <tr>
                      <th>#</th>
                      <th>Student ID</th>
                      <th>Student Full Name</th>
                      <th>Program &amp; Year</th>
                      <th>Email</th>
                      <th>Current Grade Status</th>
                      <th class="no-print">Student Profile</th>
                    </tr>
                  </thead>
                  <tbody>
                    <?php foreach ($rosterStudents as $idx => $st): 
                      $gVal = $st['is_inc'] ? 'INC' : ($st['grade'] !== null ? number_format((float)$st['grade'], 2) : 'Unencoded');
                    ?>
                    <tr>
                      <td><?php echo $idx + 1; ?></td>
                      <td><strong style="color:var(--accent);"><?php echo htmlspecialchars($st['student_id_number']); ?></strong></td>
                      <td><strong><?php echo htmlspecialchars($st['name']); ?></strong></td>
                      <td><?php echo htmlspecialchars($st['program'] . ' (' . $st['year_level'] . ')'); ?></td>
                      <td><?php echo htmlspecialchars($st['email']); ?></td>
                      <td><span class="badge <?php echo ($gVal === 'Unencoded') ? 'badge-draft' : 'badge-verified'; ?>"><?php echo $gVal; ?></span></td>
                      <td class="no-print"><a href="student_records.php?id=<?php echo $st['student_id']; ?>" class="btn-small">201 File ›</a></td>
                    </tr>
                    <?php endforeach; ?>
                  </tbody>
                </table>
              </div>
            <?php endif; ?>
          <?php endif; ?>
        </div>
      <?php endif; ?>

    </div>
  </div>

  <!-- Modal: Schedule New Class Offering -->
  <div id="addOfferingModal" style="display:none;position:fixed;top:0;left:0;right:0;bottom:0;background:rgba(15,23,42,0.6);z-index:999;">
    <div style="max-width:540px;background:#fff;border-radius:12px;margin:50px auto;padding:24px;box-shadow:0 20px 25px -5px rgba(0,0,0,0.2);max-height:90vh;overflow-y:auto;">
      <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px;">
        <h2 style="margin:0;font-size:18px;">Schedule Class Section Offering</h2>
        <button onclick="document.getElementById('addOfferingModal').style.display='none'" style="background:none;border:none;font-size:20px;cursor:pointer;">&times;</button>
      </div>
      <form method="post">
        <input type="hidden" name="action" value="add_offering">

        <div class="form-group" style="margin-bottom:12px;">
          <label>Select Course Subject *</label>
          <select name="subject_id" required>
            <option value="">-- Choose Subject --</option>
            <?php foreach ($subjectsList as $s): ?>
              <option value="<?php echo $s['id']; ?>"><?php echo htmlspecialchars($s['subject_code'] . ' — ' . $s['subject_name'] . ' (' . $s['units'] . ' units)'); ?></option>
            <?php endforeach; ?>
          </select>
        </div>

        <div class="form-grid">
          <div class="form-group">
            <label>Section Code *</label>
            <input type="text" name="section_code" placeholder="e.g. BSIT-2A" required>
          </div>
          <div class="form-group">
            <label>Room Assignment *</label>
            <input type="text" name="room" placeholder="e.g. CL-301 or IT-LAB-1" required>
          </div>
        </div>

        <div class="form-group" style="margin-bottom:12px;">
          <label>Schedule Days *</label>
          <div style="display:flex;gap:12px;flex-wrap:wrap;padding:6px 0;">
            <?php foreach ($allDays as $d): ?>
              <label style="display:inline-flex;align-items:center;gap:4px;font-size:13px;font-weight:normal;">
                <input type="checkbox" name="days[]" value="<?php echo $d; ?>"> <?php echo $d; ?>
              </label>
            <?php endforeach; ?>
          </div>
        </div>

        <div class="form-grid">
          <div class="form-group">
            <label>Start Time *</label>
            <input type="time" name="start_time" required>
          </div>
          <div class="form-group">
            <label>End Time *</label>
            <input type="time" name="end_time" required>
          </div>
        </div>

        <div class="form-grid">
          <div class="form-group">
            <label>Assigned Faculty / Instructor</label>
            <input type="text" name="instructor_name" placeholder="Prof. Dan Castillo" value="Engr. Danilo Castillo">
          </div>
          <div class="form-group">
            <label>Max Class Capacity</label>
            <input type="number" name="capacity" value="40" min="10" max="80">
          </div>
        </div>

        <div style="display:flex;justify-content:flex-end;gap:10px;margin-top:16px;">
          <button type="button" class="btn btn-secondary" onclick="document.getElementById('addOfferingModal').style.display='none'">Cancel</button>
          <button type="submit" class="btn btn-primary">Schedule Offering</button>
        </div>
      </form>
    </div>
  </div>

  <!-- Modal: Add New Subject to Catalog -->
  <div id="addSubjectModal" style="display:none;position:fixed;top:0;left:0;right:0;bottom:0;background:rgba(15,23,42,0.6);z-index:999;">
    <div style="max-width:540px;background:#fff;border-radius:12px;margin:50px auto;padding:24px;box-shadow:0 20px 25px -5px rgba(0,0,0,0.2);max-height:90vh;overflow-y:auto;">
      <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px;">
        <h2 style="margin:0;font-size:18px;">Add New Course to Catalog</h2>
        <button onclick="document.getElementById('addSubjectModal').style.display='none'" style="background:none;border:none;font-size:20px;cursor:pointer;">&times;</button>
      </div>
      <form method="post">
        <input type="hidden" name="action" value="add_subject">

        <div class="form-grid">
          <div class="form-group">
            <label>Course Code *</label>
            <input type="text" name="subject_code" placeholder="e.g. IT306" required>
          </div>
          <div class="form-group">
            <label>Credit Units *</label>
            <input type="number" step="0.5" name="units" value="3.0" required>
          </div>
        </div>

        <div class="form-group" style="margin-bottom:12px;">
          <label>Descriptive Title *</label>
          <input type="text" name="subject_name" placeholder="e.g. Web Application Security" required>
        </div>

        <div class="form-grid">
          <div class="form-group">
            <label>Lecture Hours</label>
            <input type="number" name="lec_hours" value="3">
          </div>
          <div class="form-group">
            <label>Lab Hours</label>
            <input type="number" name="lab_hours" value="0">
          </div>
        </div>

        <div class="form-group" style="margin-bottom:12px;">
          <label>Prerequisite Subject (if any)</label>
          <select name="prerequisite_subject_id">
            <option value="">None (No Prerequisite)</option>
            <?php foreach ($subjectsList as $s): ?>
              <option value="<?php echo $s['id']; ?>"><?php echo htmlspecialchars($s['subject_code'] . ' - ' . $s['subject_name']); ?></option>
            <?php endforeach; ?>
          </select>
        </div>

        <div class="form-grid">
          <div class="form-group">
            <label>Program</label>
            <select name="curriculum_program">
              <option value="BSIT">BSIT</option>
              <option value="BSCS">BSCS</option>
              <option value="BSBA">BSBA</option>
              <option value="All">General Education / All</option>
            </select>
          </div>
          <div class="form-group">
            <label>Year Level</label>
            <select name="year_level">
              <option value="1st Year">1st Year</option>
              <option value="2nd Year">2nd Year</option>
              <option value="3rd Year">3rd Year</option>
              <option value="4th Year">4th Year</option>
            </select>
          </div>
        </div>

        <div style="display:flex;justify-content:flex-end;gap:10px;margin-top:16px;">
          <button type="button" class="btn btn-secondary" onclick="document.getElementById('addSubjectModal').style.display='none'">Cancel</button>
          <button type="submit" class="btn btn-primary">Save Course</button>
        </div>
      </form>
    </div>
  </div>

</body>
</html>