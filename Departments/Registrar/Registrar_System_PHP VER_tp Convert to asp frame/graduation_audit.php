<?php
require 'config.php';

$displayName       = $_SESSION['user_name'] ?? 'Registrar';
$currentSchoolYear = get_setting($pdo, 'current_school_year', '2025-2026');

$tab = $_GET['tab'] ?? 'candidates';
if (!in_array($tab, ['candidates', 'honors', 'print_roll'], true)) {
    $tab = 'candidates';
}

$flash = get_flash();

// Handle Status Updates
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $action = $_POST['action'] ?? '';
    $studentId = (int)($_POST['student_id'] ?? 0);

    if ($action === 'certify_graduate' && $studentId > 0) {
        $pdo->prepare("UPDATE student_profile SET academic_status = 'Graduated', enrollment_status = 'Graduated' WHERE user_id = ?")
            ->execute([$studentId]);
        log_activity($pdo, $studentId, "Student officially certified as Graduated by {$displayName}.");
        set_flash("Candidate successfully certified as official graduate.");
        header("Location: graduation_audit.php?tab=candidates");
        exit;
    }
}

// Fetch Graduating Students
$candidatesRaw = $pdo->query(
    "SELECT u.id, u.student_id_number, u.name, u.email, sp.program, sp.year_level, sp.average_grade, sp.completed_units, sp.units_remaining, sp.academic_status, sp.guidance_clearance_status,
            (SELECT COUNT(*) FROM document_credentials dc WHERE dc.student_id = u.id AND dc.status != 'Verified') as unverified_docs_count,
            (SELECT COUNT(*) FROM nstp_serial_numbers nsn WHERE nsn.student_id = u.id) as has_nstp,
            (SELECT COUNT(*) FROM enrolled_subjects es JOIN grades g ON g.enrolled_subject_id = es.id JOIN enrollments e ON e.id = es.enrollment_id WHERE e.student_id = u.id AND (g.is_inc = 1 OR g.grade > 3.00)) as academic_deficiencies_count
     FROM `user` u
     JOIN student_profile sp ON sp.user_id = u.id
     WHERE sp.year_level = '4th Year' OR sp.academic_status IN ('Graduating', 'Graduated')
     ORDER BY sp.average_grade ASC, u.name ASC"
)->fetchAll();

// Process Candidate Audit & Honors
$candidateAudit = [];
$honorsList = [];

foreach ($candidatesRaw as $c) {
    $gwa = (float)($c['average_grade'] ?? 5.00);
    $deficiencies = [];

    if ((int)$c['completed_units'] < 138) {
        $deficiencies[] = "Lacks " . (144 - (int)$c['completed_units']) . " curriculum units";
    }
    if ((int)$c['academic_deficiencies_count'] > 0) {
        $deficiencies[] = "Has pending INC or failing mark";
    }
    if ((int)$c['unverified_docs_count'] > 0) {
        $deficiencies[] = "201 credentials incomplete";
    }
    if ((int)$c['has_nstp'] === 0) {
        $deficiencies[] = "Missing NSTP Serial Number";
    }
    if ($c['guidance_clearance_status'] !== 'Cleared') {
        $deficiencies[] = "Guidance clearance hold";
    }

    $isEligible = empty($deficiencies);

    // Latin Honors Qualification Check (CHED Rules)
    $honorTitle = null;
    $honorBadge = 'badge-on-leave';

    if ($gwa >= 1.00 && $gwa <= 1.20 && (int)$c['academic_deficiencies_count'] === 0) {
        $honorTitle = 'Summa Cum Laude';
        $honorBadge = 'badge-active';
    } elseif ($gwa > 1.20 && $gwa <= 1.45 && (int)$c['academic_deficiencies_count'] === 0) {
        $honorTitle = 'Magna Cum Laude';
        $honorBadge = 'badge-open';
    } elseif ($gwa > 1.45 && $gwa <= 1.75 && (int)$c['academic_deficiencies_count'] === 0) {
        $honorTitle = 'Cum Laude';
        $honorBadge = 'badge-open';
    }

    $auditItem = array_merge($c, [
        'is_eligible'  => $isEligible,
        'deficiencies' => $deficiencies,
        'honor_title'  => $honorTitle,
        'honor_badge'  => $honorBadge
    ]);

    $candidateAudit[] = $auditItem;

    if ($honorTitle && $isEligible) {
        $honorsList[] = $auditItem;
    }
}
?>
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Graduation Audit &amp; Latin Honors - Registrar</title>
  <link rel="stylesheet" href="dashboard.css">
</head>
<body class="dashboard-body">

  <?php render_sidebar('graduation_audit.php'); ?>

  <div class="dash-main">
    <?php render_topbar($pdo, 'Graduation Audit & Honors'); ?>

    <div class="dash-content">
      <?php if ($flash): ?>
        <div class="alert-banner alert-<?php echo htmlspecialchars($flash['type']); ?>">
          <span><?php echo htmlspecialchars($flash['message']); ?></span>
        </div>
      <?php endif; ?>

      <div class="dash-heading-row">
        <div>
          <h1 class="dash-heading">🎓 Graduation Audit &amp; Academic Honors</h1>
          <p class="dash-subheading">Evaluate prospective graduates, check scholastic deficiencies, compute Latin Honors, and generate commencement rolls.</p>
        </div>
        <div>
          <?php if ($tab !== 'print_roll'): ?>
            <a href="graduation_audit.php?tab=print_roll" class="btn btn-secondary">🖨️ Printable Commencement Roll</a>
          <?php else: ?>
            <button onclick="window.print()" class="btn btn-primary">🖨️ Print Official List</button>
          <?php endif; ?>
        </div>
      </div>

      <!-- Tab Navigation -->
      <div class="tab-nav no-print">
        <a href="graduation_audit.php?tab=candidates" class="tab-link <?php echo $tab === 'candidates' ? 'active' : ''; ?>">
          <span>📋 Candidates Audit &amp; Clearance (<?php echo count($candidateAudit); ?>)</span>
        </a>
        <a href="graduation_audit.php?tab=honors" class="tab-link <?php echo $tab === 'honors' ? 'active' : ''; ?>">
          <span>🏆 Latin Honors Ranking (<?php echo count($honorsList); ?>)</span>
        </a>
        <a href="graduation_audit.php?tab=print_roll" class="tab-link <?php echo $tab === 'print_roll' ? 'active' : ''; ?>">
          <span>📜 Official Commencement Roll</span>
        </a>
      </div>

      <!-- TAB 1: CANDIDATES AUDIT -->
      <?php if ($tab === 'candidates'): ?>
        <div class="dash-panel">
          <div class="dash-panel-header">
            <h2>Prospective Graduation Candidates Evaluation (AY <?php echo htmlspecialchars($currentSchoolYear); ?>)</h2>
          </div>
          <div class="table-responsive">
            <table class="table-simple">
              <thead>
                <tr>
                  <th>Student ID</th>
                  <th>Candidate Name</th>
                  <th>Program</th>
                  <th>GWA</th>
                  <th>Units Earned</th>
                  <th>NSTP</th>
                  <th>Deficiency / Clearance Audit</th>
                  <th>Honors Potential</th>
                  <th style="text-align:right;">Action</th>
                </tr>
              </thead>
              <tbody>
                <?php foreach ($candidateAudit as $ca): ?>
                <tr>
                  <td><strong style="color:var(--accent);"><?php echo htmlspecialchars($ca['student_id_number']); ?></strong></td>
                  <td>
                    <strong><?php echo htmlspecialchars($ca['name']); ?></strong>
                    <div style="font-size:11px;color:#64748b;"><?php echo htmlspecialchars($ca['email']); ?></div>
                  </td>
                  <td><?php echo htmlspecialchars($ca['program']); ?></td>
                  <td><strong style="color:var(--accent);"><?php echo number_format((float)$ca['average_grade'], 2); ?></strong></td>
                  <td><?php echo (int)$ca['completed_units']; ?> / 144</td>
                  <td>
                    <?php if ($ca['has_nstp'] > 0): ?>
                      <span class="badge badge-verified">✔️ Certified</span>
                    <?php else: ?>
                      <span class="badge badge-missing">Missing</span>
                    <?php endif; ?>
                  </td>
                  <td>
                    <?php if ($ca['is_eligible']): ?>
                      <span class="badge badge-verified">✅ Cleared for Graduation</span>
                    <?php else: ?>
                      <span class="badge badge-missing">Deficient (<?php echo count($ca['deficiencies']); ?>)</span>
                      <div style="font-size:11px;color:#dc2626;margin-top:2px;">
                        <?php echo htmlspecialchars(implode(', ', $ca['deficiencies'])); ?>
                      </div>
                    <?php endif; ?>
                  </td>
                  <td>
                    <?php if ($ca['honor_title']): ?>
                      <span class="badge <?php echo $ca['honor_badge']; ?>">🏆 <?php echo $ca['honor_title']; ?></span>
                    <?php else: ?>
                      <span style="color:#94a3b8;font-size:12px;">Regular Graduate</span>
                    <?php endif; ?>
                  </td>
                  <td style="text-align:right;">
                    <?php if ($ca['academic_status'] === 'Graduated'): ?>
                      <span class="badge badge-graduated">Graduated</span>
                    <?php elseif ($ca['is_eligible']): ?>
                      <form method="post" style="display:inline;">
                        <input type="hidden" name="action" value="certify_graduate">
                        <input type="hidden" name="student_id" value="<?php echo $ca['id']; ?>">
                        <button type="submit" class="btn-small primary" onclick="return confirm('Officially certify this student as a graduate?')">🎓 Certify</button>
                      </form>
                    <?php else: ?>
                      <a href="student_records.php?id=<?php echo $ca['id']; ?>" class="btn-small">Audit 201 ›</a>
                    <?php endif; ?>
                  </td>
                </tr>
                <?php endforeach; ?>
              </tbody>
            </table>
          </div>
        </div>

      <!-- TAB 2: LATIN HONORS EVALUATION -->
      <?php elseif ($tab === 'honors'): ?>
        <div class="dash-panel">
          <div class="dash-panel-header">
            <h2>Official Academic Honors Ranking (CHED Memorandum Order Standard)</h2>
          </div>
          <p style="font-size:13px;color:#64748b;margin-top:0;">
            Honors criteria: <strong>Summa Cum Laude (1.00–1.20)</strong> &middot; <strong>Magna Cum Laude (1.21–1.45)</strong> &middot; <strong>Cum Laude (1.46–1.75)</strong>. Requires zero failing marks, completion of curriculum units, and full 201 &amp; guidance clearances.
          </p>

          <?php if (empty($honorsList)): ?>
            <p style="text-align:center;padding:30px;color:#64748b;">No candidates currently qualify for Latin Honors.</p>
          <?php else: ?>
            <div class="table-responsive">
              <table class="table-simple">
                <thead>
                  <tr>
                    <th>Rank</th>
                    <th>Award Title</th>
                    <th>Candidate Name</th>
                    <th>Student ID</th>
                    <th>Degree Program</th>
                    <th>Cumulative GWA</th>
                    <th>Scholastic Standing</th>
                  </tr>
                </thead>
                <tbody>
                  <?php foreach ($honorsList as $idx => $h): ?>
                  <tr>
                    <td><strong style="font-size:15px;color:var(--accent);">#<?php echo $idx + 1; ?></strong></td>
                    <td>
                      <span class="badge <?php echo $h['honor_badge']; ?>" style="font-size:12px;padding:5px 12px;">
                        🏅 <?php echo $h['honor_title']; ?>
                      </span>
                    </td>
                    <td><strong style="font-size:14px;"><?php echo htmlspecialchars($h['name']); ?></strong></td>
                    <td><?php echo htmlspecialchars($h['student_id_number']); ?></td>
                    <td><?php echo htmlspecialchars($h['program']); ?></td>
                    <td><strong style="font-size:15px;color:var(--success);"><?php echo number_format((float)$h['average_grade'], 2); ?></strong></td>
                    <td><span class="badge badge-verified">Full Clearance Met</span></td>
                  </tr>
                  <?php endforeach; ?>
                </tbody>
              </table>
            </div>
          <?php endif; ?>
        </div>

      <!-- TAB 3: PRINTABLE COMMENCEMENT ROLL -->
      <?php elseif ($tab === 'print_roll'): ?>
        <div class="official-document-sheet">
          <div class="doc-header">
            <div class="doc-institution">METROPOLITAN STATE UNIVERSITY</div>
            <div class="doc-sub">Office of the University Registrar &middot; Manila, Philippines</div>
            <div class="doc-title">OFFICIAL COMMENCEMENT ROLL OF GRADUATES</div>
            <div style="font-size:12px;color:#555;">ACADEMIC YEAR <?php echo htmlspecialchars($currentSchoolYear); ?></div>
          </div>

          <div style="margin-bottom:24px;">
            <p style="font-size:13px;line-height:1.6;margin:0 0 16px;">
              This is to certify that the following candidates have satisfactorily completed all academic, non-academic, and regulatory requirements prescribed by the Commission on Higher Education (CHED) and the University Board of Regents for the conferment of their respective baccalaureate degrees:
            </p>

            <table style="width:100%;border-collapse:collapse;font-size:12px;margin-bottom:24px;">
              <thead>
                <tr style="border-bottom:2px solid #000;text-align:left;">
                  <th style="padding:6px;">No.</th>
                  <th style="padding:6px;">Student ID</th>
                  <th style="padding:6px;">Full Name</th>
                  <th style="padding:6px;">Degree Program</th>
                  <th style="padding:6px;">Cumulative GWA</th>
                  <th style="padding:6px;">Academic Distinction</th>
                </tr>
              </thead>
              <tbody>
                <?php foreach ($candidateAudit as $idx => $c): ?>
                <tr style="border-bottom:1px solid #ddd;">
                  <td style="padding:6px;"><?php echo $idx + 1; ?></td>
                  <td style="padding:6px;"><?php echo htmlspecialchars($c['student_id_number']); ?></td>
                  <td style="padding:6px;font-weight:bold;"><?php echo htmlspecialchars($c['name']); ?></td>
                  <td style="padding:6px;"><?php echo htmlspecialchars($c['program']); ?></td>
                  <td style="padding:6px;"><?php echo number_format((float)$c['average_grade'], 2); ?></td>
                  <td style="padding:6px;font-weight:bold;"><?php echo $c['honor_title'] ? $c['honor_title'] : '—'; ?></td>
                </tr>
                <?php endforeach; ?>
              </tbody>
            </table>
          </div>

          <div class="doc-signatories">
            <div class="doc-sign-box">
              <div class="doc-seal-box">
                OFFICIAL DRY SEAL<br>OF THE REGISTRAR
              </div>
            </div>

            <div class="doc-sign-box">
              <div class="doc-sign-line">
                DR. ROSALINDA M. SANTOS, Ed.D.<br>
                <span style="font-weight:normal;font-size:11px;">University Registrar</span>
              </div>
            </div>

            <div class="doc-sign-box">
              <div class="doc-sign-line">
                DR. VICENTE C. SANDOVAL, Ph.D.<br>
                <span style="font-weight:normal;font-size:11px;">University President</span>
              </div>
            </div>
          </div>
        </div>
      <?php endif; ?>

    </div>
  </div>

</body>
</html>