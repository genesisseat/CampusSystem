<?php
require 'config.php';

$displayName       = $_SESSION['user_name'] ?? 'Registrar';
$currentSchoolYear = get_setting($pdo, 'current_school_year', '2025-2026');
$currentSemester   = get_setting($pdo, 'current_semester', '1st Semester');
$schoolName        = get_setting($pdo, 'school_name', 'Metropolitan State University');
$schoolCode        = get_setting($pdo, 'school_code', 'MSU-0422');

$tab = $_GET['tab'] ?? 'so';
if (!in_array($tab, ['so', 'nstp', 'masterlist'], true)) {
    $tab = 'so';
}

$flash = get_flash();

// Handle CSV Export for CHED Masterlist
if (isset($_GET['export']) && $_GET['export'] === 'csv') {
    $stmtChed = $pdo->prepare(
        "SELECT u.student_id_number, u.name, sp.gender, sp.program, sp.year_level, e.total_units, e.enrollment_type, e.status
         FROM enrollments e
         JOIN `user` u ON u.id = e.student_id
         LEFT JOIN student_profile sp ON sp.user_id = u.id
         WHERE e.school_year = ? AND e.semester = ? AND e.status = 'active'
         ORDER BY sp.program ASC, sp.year_level ASC, u.name ASC"
    );
    $stmtChed->execute([$currentSchoolYear, $currentSemester]);
    $exportRows = $stmtChed->fetchAll();

    header('Content-Type: text/csv; charset=utf-8');
    header('Content-Disposition: attachment; filename=CHED_Masterlist_' . str_replace(' ', '_', $currentSchoolYear . '_' . $currentSemester) . '.csv');

    $output = fopen('php://output', 'w');
    fputcsv($output, ['CHED Institutional Code', $schoolCode]);
    fputcsv($output, ['Higher Education Institution', $schoolName]);
    fputcsv($output, ['Academic School Year', $currentSchoolYear]);
    fputcsv($output, ['Academic Semester', $currentSemester]);
    fputcsv($output, []); // blank line
    fputcsv($output, ['Student ID Number', 'Full Legal Name', 'Sex', 'Degree Program', 'Year Level', 'Total Units Enrolled', 'Matriculation Type', 'Enrollment Status']);

    foreach ($exportRows as $er) {
        fputcsv($output, [
            $er['student_id_number'],
            $er['name'],
            $er['gender'],
            $er['program'],
            $er['year_level'],
            $er['total_units'],
            strtoupper($er['enrollment_type']),
            strtoupper($er['status'])
        ]);
    }
    fclose($output);
    exit;
}

// Handle Form Submissions
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $action = $_POST['action'] ?? '';

    try {
        // 1. Record / Apply CHED S.O.
        if ($action === 'record_so') {
            $studentId = (int)($_POST['student_id'] ?? 0);
            $soNumber  = trim($_POST['so_number'] ?? '');
            $seriesYr  = trim($_POST['series_year'] ?? date('Y'));
            $program   = trim($_POST['program'] ?? 'BS Information Technology');
            $status    = trim($_POST['status'] ?? 'Applied');
            $dateApp   = $_POST['date_applied'] ?: date('Y-m-d');
            $dateIss   = !empty($_POST['date_issued']) ? $_POST['date_issued'] : null;
            $remarks   = trim($_POST['remarks'] ?? '');

            if ($studentId && $soNumber) {
                $stmt = $pdo->prepare(
                    "INSERT INTO ched_special_orders 
                     (student_id, so_number, series_year, program, date_applied, date_issued, status, remarks)
                     VALUES (?, ?, ?, ?, ?, ?, ?, ?)"
                );
                $stmt->execute([$studentId, $soNumber, $seriesYr, $program, $dateApp, $dateIss, $status, $remarks]);
                log_activity($pdo, $studentId, "CHED Special Order recorded ({$soNumber}) by {$displayName}.");
                set_flash("CHED Special Order recorded successfully.");
            } else {
                set_flash("Please provide student and S.O. number.", 'error');
            }
            header("Location: government_compliance.php?tab=so");
            exit;
        }

        // 2. Record NSTP Serial Number
        if ($action === 'record_nstp') {
            $studentId = (int)($_POST['student_id'] ?? 0);
            $component = trim($_POST['nstp_component'] ?? 'CWTS');
            $serialNum = strtoupper(trim($_POST['serial_number'] ?? ''));
            $dateIss   = $_POST['date_issued'] ?: date('Y-m-d');
            $remarks   = trim($_POST['remarks'] ?? '');

            if ($studentId && $serialNum) {
                $stmt = $pdo->prepare(
                    "INSERT INTO nstp_serial_numbers 
                     (student_id, nstp_component, serial_number, date_issued, remarks)
                     VALUES (?, ?, ?, ?, ?)"
                );
                $stmt->execute([$studentId, $component, $serialNum, $dateIss, $remarks]);
                log_activity($pdo, $studentId, "NSTP Serial Number recorded ({$serialNum}) by {$displayName}.");
                set_flash("NSTP Serial Number recorded successfully.");
            } else {
                set_flash("Please provide student and serial number.", 'error');
            }
            header("Location: government_compliance.php?tab=nstp");
            exit;
        }
    } catch (Throwable $e) {
        set_flash("Error: " . $e->getMessage(), 'error');
        header("Location: government_compliance.php?tab={$tab}");
        exit;
    }
}

// Dropdown students list
$graduatingStudents = $pdo->query("SELECT u.id, u.student_id_number, u.name, sp.program FROM `user` u JOIN student_profile sp ON sp.user_id = u.id WHERE u.role = 'student' ORDER BY u.name ASC")->fetchAll();

// Queries
$soRecords = $pdo->query(
    "SELECT cso.*, u.name as student_name, u.student_id_number, u.email
     FROM ched_special_orders cso
     JOIN `user` u ON u.id = cso.student_id
     ORDER BY cso.date_applied DESC"
)->fetchAll();

$nstpRecords = $pdo->query(
    "SELECT nsn.*, u.name as student_name, u.student_id_number, sp.program
     FROM nstp_serial_numbers nsn
     JOIN `user` u ON u.id = nsn.student_id
     LEFT JOIN student_profile sp ON sp.user_id = u.id
     ORDER BY nsn.date_issued DESC"
)->fetchAll();

$masterlist = $pdo->prepare(
    "SELECT e.id, e.total_units, e.enrollment_type, e.status as enrollment_status,
            u.student_id_number, u.name, sp.gender, sp.program, sp.year_level
     FROM enrollments e
     JOIN `user` u ON u.id = e.student_id
     LEFT JOIN student_profile sp ON sp.user_id = u.id
     WHERE e.school_year = ? AND e.semester = ? AND e.status = 'active'
     ORDER BY sp.program ASC, u.name ASC"
);
$masterlist->execute([$currentSchoolYear, $currentSemester]);
$enrolledMasterlist = $masterlist->fetchAll();
?>
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Government Compliance &amp; CHED Reporting - Registrar</title>
  <link rel="stylesheet" href="dashboard.css">
</head>
<body class="dashboard-body">

  <?php render_sidebar('government_compliance.php'); ?>

  <div class="dash-main">
    <?php render_topbar($pdo, 'Government Compliance'); ?>

    <div class="dash-content">
      <?php if ($flash): ?>
        <div class="alert-banner alert-<?php echo htmlspecialchars($flash['type']); ?>">
          <span><?php echo htmlspecialchars($flash['message']); ?></span>
        </div>
      <?php endif; ?>

      <div class="dash-heading-row">
        <div>
          <h1 class="dash-heading">🏛️ Government Regulatory Compliance (CHED / DND)</h1>
          <p class="dash-subheading">Manage Special Orders (S.O.), official NSTP/ROTC serial registries, and generate official CHED Electronic Enrolment Lists.</p>
        </div>
        <div>
          <?php if ($tab === 'so'): ?>
            <button class="btn btn-primary" onclick="document.getElementById('soModal').style.display='block'">➕ Register Special Order (S.O.)</button>
          <?php elseif ($tab === 'nstp'): ?>
            <button class="btn btn-primary" onclick="document.getElementById('nstpModal').style.display='block'">➕ Issue NSTP Serial</button>
          <?php elseif ($tab === 'masterlist'): ?>
            <a href="government_compliance.php?export=csv" class="btn btn-secondary">📥 Export CHED CSV Masterlist</a>
          <?php endif; ?>
        </div>
      </div>

      <!-- Tab Navigation -->
      <div class="tab-nav no-print">
        <a href="government_compliance.php?tab=so" class="tab-link <?php echo $tab === 'so' ? 'active' : ''; ?>">
          <span>📜 CHED Special Orders (<?php echo count($soRecords); ?>)</span>
        </a>
        <a href="government_compliance.php?tab=nstp" class="tab-link <?php echo $tab === 'nstp' ? 'active' : ''; ?>">
          <span>🎖️ NSTP / ROTC Serial Numbers (<?php echo count($nstpRecords); ?>)</span>
        </a>
        <a href="government_compliance.php?tab=masterlist" class="tab-link <?php echo $tab === 'masterlist' ? 'active' : ''; ?>">
          <span>📑 CHED Electronic Enrolment List (<?php echo count($enrolledMasterlist); ?>)</span>
        </a>
      </div>

      <!-- TAB 1: CHED SPECIAL ORDERS -->
      <?php if ($tab === 'so'): ?>
        <div class="dash-panel">
          <div class="dash-panel-header">
            <h2>CHED Special Orders (S.O.) Registry</h2>
          </div>
          <p style="font-size:13px;color:#64748b;margin-top:0;">
            Mandatory government authority issued by the Commission on Higher Education certifying degree eligibility for college graduates.
          </p>
          <div class="table-responsive">
            <table class="table-simple">
              <thead>
                <tr>
                  <th>S.O. Number</th>
                  <th>Series</th>
                  <th>Graduate Name</th>
                  <th>Degree Program</th>
                  <th>Date Applied</th>
                  <th>Date Issued</th>
                  <th>Status</th>
                  <th>Remarks</th>
                </tr>
              </thead>
              <tbody>
                <?php foreach ($soRecords as $so): ?>
                <tr>
                  <td><strong style="color:var(--accent);font-size:13px;"><?php echo htmlspecialchars($so['so_number']); ?></strong></td>
                  <td><?php echo htmlspecialchars($so['series_year']); ?></td>
                  <td>
                    <strong><?php echo htmlspecialchars($so['student_name']); ?></strong>
                    <div style="font-size:11px;color:#64748b;"><?php echo htmlspecialchars($so['student_id_number']); ?></div>
                  </td>
                  <td><?php echo htmlspecialchars($so['program']); ?></td>
                  <td><?php echo date('M d, Y', strtotime($so['date_applied'])); ?></td>
                  <td><?php echo $so['date_issued'] ? date('M d, Y', strtotime($so['date_issued'])) : '<span style="color:#94a3b8;">Pending Approval</span>'; ?></td>
                  <td><span class="badge badge-<?php echo strtolower(str_replace(' ', '_', $so['status'])); ?>"><?php echo htmlspecialchars($so['status']); ?></span></td>
                  <td style="font-size:12px;color:#475569;"><?php echo htmlspecialchars($so['remarks'] ?: '—'); ?></td>
                </tr>
                <?php endforeach; ?>
              </tbody>
            </table>
          </div>
        </div>

      <!-- TAB 2: NSTP SERIAL NUMBERS -->
      <?php elseif ($tab === 'nstp'): ?>
        <div class="dash-panel">
          <div class="dash-panel-header">
            <h2>National Service Training Program (NSTP/ROTC) Serial Number Registry</h2>
          </div>
          <p style="font-size:13px;color:#64748b;margin-top:0;">
            Official DND/CHED certified completion serial numbers for Republic Act 9163 compliance.
          </p>
          <div class="table-responsive">
            <table class="table-simple">
              <thead>
                <tr>
                  <th>Official Serial Number</th>
                  <th>Component</th>
                  <th>Student Name</th>
                  <th>Student ID</th>
                  <th>Degree Program</th>
                  <th>Date Issued</th>
                  <th>Remarks</th>
                </tr>
              </thead>
              <tbody>
                <?php foreach ($nstpRecords as $nr): ?>
                <tr>
                  <td><strong class="token-chip" style="color:var(--primary);"><?php echo htmlspecialchars($nr['serial_number']); ?></strong></td>
                  <td><span class="badge badge-open"><?php echo htmlspecialchars($nr['nstp_component']); ?></span></td>
                  <td><strong><?php echo htmlspecialchars($nr['student_name']); ?></strong></td>
                  <td><?php echo htmlspecialchars($nr['student_id_number']); ?></td>
                  <td><?php echo htmlspecialchars($nr['program']); ?></td>
                  <td><?php echo date('M d, Y', strtotime($nr['date_issued'])); ?></td>
                  <td style="font-size:12px;color:#475569;"><?php echo htmlspecialchars($nr['remarks'] ?: 'Official completion verified'); ?></td>
                </tr>
                <?php endforeach; ?>
              </tbody>
            </table>
          </div>
        </div>

      <!-- TAB 3: CHED ELECTRONIC ENROLMENT LIST -->
      <?php elseif ($tab === 'masterlist'): ?>
        <div class="dash-panel">
          <div class="dash-panel-header">
            <h2>CHED Electronic Enrolment List (EEL) — AY <?php echo htmlspecialchars($currentSchoolYear); ?> <?php echo htmlspecialchars($currentSemester); ?></h2>
            <div class="no-print">
              <button onclick="window.print()" class="btn btn-secondary">🖨️ Print Formatted Report</button>
            </div>
          </div>
          <div style="font-size:12px;color:#475569;margin-bottom:16px;">
            <strong>Institution:</strong> <?php echo htmlspecialchars($schoolName); ?> &middot; <strong>Code:</strong> <?php echo htmlspecialchars($schoolCode); ?>
          </div>

          <div class="table-responsive">
            <table class="table-simple">
              <thead>
                <tr>
                  <th>No.</th>
                  <th>Student ID Number</th>
                  <th>Full Legal Name</th>
                  <th>Sex</th>
                  <th>Program</th>
                  <th>Year</th>
                  <th>Enrolled Units</th>
                  <th>Type</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                <?php foreach ($enrolledMasterlist as $idx => $m): ?>
                <tr>
                  <td><?php echo $idx + 1; ?></td>
                  <td><strong style="color:var(--accent);"><?php echo htmlspecialchars($m['student_id_number']); ?></strong></td>
                  <td><strong><?php echo htmlspecialchars($m['name']); ?></strong></td>
                  <td><?php echo htmlspecialchars($m['gender']); ?></td>
                  <td><?php echo htmlspecialchars($m['program']); ?></td>
                  <td><?php echo htmlspecialchars($m['year_level']); ?></td>
                  <td><strong><?php echo htmlspecialchars($m['total_units']); ?> units</strong></td>
                  <td><?php echo htmlspecialchars(ucfirst($m['enrollment_type'])); ?></td>
                  <td><span class="badge badge-active">Officially Enrolled</span></td>
                </tr>
                <?php endforeach; ?>
              </tbody>
            </table>
          </div>
        </div>
      <?php endif; ?>

    </div>
  </div>

  <!-- Modal: Record CHED S.O. -->
  <div id="soModal" style="display:none;position:fixed;top:0;left:0;right:0;bottom:0;background:rgba(15,23,42,0.6);z-index:999;">
    <div style="max-width:540px;background:#fff;border-radius:12px;margin:50px auto;padding:24px;box-shadow:0 20px 25px -5px rgba(0,0,0,0.2);max-height:90vh;overflow-y:auto;">
      <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px;">
        <h2 style="margin:0;font-size:18px;">Register CHED Special Order (S.O.)</h2>
        <button onclick="document.getElementById('soModal').style.display='none'" style="background:none;border:none;font-size:20px;cursor:pointer;">&times;</button>
      </div>
      <form method="post">
        <input type="hidden" name="action" value="record_so">

        <div class="form-group" style="margin-bottom:12px;">
          <label>Select Candidate Student *</label>
          <select name="student_id" required>
            <option value="">-- Choose Candidate --</option>
            <?php foreach ($graduatingStudents as $s): ?>
              <option value="<?php echo $s['id']; ?>"><?php echo htmlspecialchars($s['name'] . ' (' . $s['student_id_number'] . ') — ' . $s['program']); ?></option>
            <?php endforeach; ?>
          </select>
        </div>

        <div class="form-grid">
          <div class="form-group">
            <label>Special Order (S.O.) Number *</label>
            <input type="text" name="so_number" placeholder="e.g. SO (B) No. 04-2025-10495" required>
          </div>
          <div class="form-group">
            <label>Series Year *</label>
            <input type="text" name="series_year" value="<?php echo date('Y'); ?>" required>
          </div>
        </div>

        <div class="form-grid">
          <div class="form-group">
            <label>Degree Program</label>
            <input type="text" name="program" value="BS Information Technology">
          </div>
          <div class="form-group">
            <label>CHED Processing Status</label>
            <select name="status">
              <option value="Applied">Applied</option>
              <option value="Under Review">Under Review</option>
              <option value="Issued">Issued / Approved</option>
              <option value="Pending Requirements">Pending Requirements</option>
            </select>
          </div>
        </div>

        <div class="form-grid">
          <div class="form-group">
            <label>Date Applied</label>
            <input type="date" name="date_applied" value="<?php echo date('Y-m-d'); ?>">
          </div>
          <div class="form-group">
            <label>Date Issued (if already released)</label>
            <input type="date" name="date_issued">
          </div>
        </div>

        <div class="form-group" style="margin-bottom:20px;">
          <label>CHED Endorsement Remarks</label>
          <textarea name="remarks" rows="2" placeholder="e.g. Endorsed by CHED NCR Higher Education Division"></textarea>
        </div>

        <div style="display:flex;justify-content:flex-end;gap:10px;">
          <button type="button" class="btn btn-secondary" onclick="document.getElementById('soModal').style.display='none'">Cancel</button>
          <button type="submit" class="btn btn-primary">Save Special Order</button>
        </div>
      </form>
    </div>
  </div>

  <!-- Modal: Record NSTP Serial -->
  <div id="nstpModal" style="display:none;position:fixed;top:0;left:0;right:0;bottom:0;background:rgba(15,23,42,0.6);z-index:999;">
    <div style="max-width:540px;background:#fff;border-radius:12px;margin:50px auto;padding:24px;box-shadow:0 20px 25px -5px rgba(0,0,0,0.2);">
      <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px;">
        <h2 style="margin:0;font-size:18px;">Register Official NSTP/ROTC Serial Number</h2>
        <button onclick="document.getElementById('nstpModal').style.display='none'" style="background:none;border:none;font-size:20px;cursor:pointer;">&times;</button>
      </div>
      <form method="post">
        <input type="hidden" name="action" value="record_nstp">

        <div class="form-group" style="margin-bottom:12px;">
          <label>Select Student *</label>
          <select name="student_id" required>
            <option value="">-- Choose Student --</option>
            <?php foreach ($graduatingStudents as $s): ?>
              <option value="<?php echo $s['id']; ?>"><?php echo htmlspecialchars($s['name'] . ' (' . $s['student_id_number'] . ')'); ?></option>
            <?php endforeach; ?>
          </select>
        </div>

        <div class="form-grid">
          <div class="form-group">
            <label>NSTP Component *</label>
            <select name="nstp_component">
              <option value="CWTS">CWTS (Civic Welfare Training)</option>
              <option value="ROTC">ROTC (Reserve Officers' Training)</option>
              <option value="LTS">LTS (Literacy Training Service)</option>
            </select>
          </div>
          <div class="form-group">
            <label>Official Serial Number *</label>
            <input type="text" name="serial_number" placeholder="e.g. NSTP-CWTS-2024-NCR-11500" required>
          </div>
        </div>

        <div class="form-group" style="margin-bottom:14px;">
          <label>Date Issued</label>
          <input type="date" name="date_issued" value="<?php echo date('Y-m-d'); ?>">
        </div>

        <div class="form-group" style="margin-bottom:20px;">
          <label>Remarks</label>
          <input type="text" name="remarks" placeholder="e.g. Certified by University NSTP Directorate">
        </div>

        <div style="display:flex;justify-content:flex-end;gap:10px;">
          <button type="button" class="btn btn-secondary" onclick="document.getElementById('nstpModal').style.display='none'">Cancel</button>
          <button type="submit" class="btn btn-primary">Save Serial Number</button>
        </div>
      </form>
    </div>
  </div>

</body>
</html>