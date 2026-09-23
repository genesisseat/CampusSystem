<?php
require 'config.php';

$displayName = $_SESSION['user_name'] ?? 'Registrar';
$registrarId = $_SESSION['user_id'] ?? 1;

$tab = $_GET['tab'] ?? 'queue';
if (!in_array($tab, ['queue', 'issue', 'print_doc'], true)) {
    $tab = 'queue';
}

$flash = get_flash();

// Handle Form Submissions
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $action = $_POST['action'] ?? '';

    try {
        // 1. Issue New Document Request
        if ($action === 'create_request') {
            $studentId = (int)($_POST['student_id'] ?? 0);
            $docType   = trim($_POST['document_type'] ?? '');
            $purpose   = trim($_POST['purpose'] ?? '');
            $copies    = (int)($_POST['copies'] ?? 1);
            $remarks   = trim($_POST['remarks'] ?? '');

            if ($studentId > 0 && $docType !== '') {
                $tokenPrefix = strtoupper(substr(preg_replace('/[^A-Za-z]/', '', $docType), 0, 3));
                $qrToken = 'MSU-' . $tokenPrefix . '-' . date('Y') . '-' . strtoupper(bin2hex(random_bytes(3)));

                $stmt = $pdo->prepare(
                    "INSERT INTO transcript_requests 
                     (student_id, document_type, purpose, copies, status, qr_code_token, remarks, requested_at)
                     VALUES (?, ?, ?, ?, 'pending', ?, ?, NOW())"
                );
                $stmt->execute([$studentId, $docType, $purpose, $copies, $qrToken, $remarks]);

                log_activity($pdo, $studentId, "Document request for {$docType} created (Token: {$qrToken}) by {$displayName}.");
                set_flash("Document request successfully logged with Tracking Token: {$qrToken}");
            } else {
                set_flash("Please select student and document type.", 'error');
            }
            header("Location: document_processing.php?tab=queue");
            exit;
        }

        // 2. Update Request Status Lifecycle
        if ($action === 'update_status') {
            $reqId     = (int)($_POST['request_id'] ?? 0);
            $newStatus = trim($_POST['status'] ?? '');
            $remarks   = trim($_POST['remarks'] ?? '');

            $validStatuses = ['pending', 'processing', 'ready', 'released', 'rejected'];
            if ($reqId > 0 && in_array($newStatus, $validStatuses, true)) {
                $extraSql = '';
                if ($newStatus === 'processing') {
                    $extraSql = ", processed_at = NOW()";
                } elseif ($newStatus === 'released') {
                    $extraSql = ", released_at = NOW()";
                }

                $stmt = $pdo->prepare("UPDATE transcript_requests SET status = ?, remarks = ? {$extraSql} WHERE id = ?");
                $stmt->execute([$newStatus, $remarks, $reqId]);

                $stmtStud = $pdo->prepare("SELECT student_id, document_type FROM transcript_requests WHERE id = ?");
                $stmtStud->execute([$reqId]);
                $sRow = $stmtStud->fetch();
                if ($sRow) {
                    log_activity($pdo, $sRow['student_id'], "Document request #{$reqId} ({$sRow['document_type']}) marked as {$newStatus} by {$displayName}.");
                }

                set_flash("Request #{$reqId} status updated to " . ucfirst($newStatus) . ".");
            }
            header("Location: document_processing.php?tab=queue");
            exit;
        }
    } catch (Throwable $e) {
        set_flash("Error: " . $e->getMessage(), 'error');
        header("Location: document_processing.php?tab={$tab}");
        exit;
    }
}

// Students List for Issuance
$studentsList = $pdo->query("SELECT u.id, u.student_id_number, u.name, sp.program FROM `user` u JOIN student_profile sp ON sp.user_id = u.id WHERE u.role = 'student' ORDER BY u.name ASC")->fetchAll();

// Fetch Document Requests Queue
$statusFilter = trim($_GET['status_filter'] ?? '');
$sql = "SELECT tr.*, u.name as student_name, u.student_id_number, u.email, sp.program, sp.year_level
        FROM transcript_requests tr
        JOIN `user` u ON u.id = tr.student_id
        LEFT JOIN student_profile sp ON sp.user_id = u.id";
$params = [];
if ($statusFilter !== '') {
    $sql .= " WHERE tr.status = ?";
    $params[] = $statusFilter;
}
$sql .= " ORDER BY tr.requested_at DESC";
$stmtQ = $pdo->prepare($sql);
$stmtQ->execute($params);
$requestsQueue = $stmtQ->fetchAll();

// Print Document Logic
$printReqId = isset($_GET['req_id']) ? (int)$_GET['req_id'] : 0;
$printData = null;
$printStudentGrades = [];
if ($printReqId > 0) {
    $stmtP = $pdo->prepare(
        "SELECT tr.*, u.id as student_id, u.name as student_name, u.student_id_number, u.email, sp.program, sp.year_level, sp.average_grade, sp.birth_date, sp.address, sp.gender
         FROM transcript_requests tr
         JOIN `user` u ON u.id = tr.student_id
         LEFT JOIN student_profile sp ON sp.user_id = u.id
         WHERE tr.id = ?"
    );
    $stmtP->execute([$printReqId]);
    $printData = $stmtP->fetch();

    if ($printData) {
        // Fetch student's official grades
        $stmtG = $pdo->prepare(
            "SELECT s.subject_code, s.subject_name, s.units, e.school_year, e.semester, g.grade, g.is_inc, g.remarks
             FROM enrolled_subjects es
             JOIN enrollments e ON e.id = es.enrollment_id
             JOIN class_offerings co ON co.id = es.class_offering_id
             JOIN subjects s ON s.id = co.subject_id
             LEFT JOIN grades g ON g.enrolled_subject_id = es.id
             WHERE e.student_id = ?
             ORDER BY e.school_year ASC, e.semester ASC, s.subject_code ASC"
        );
        $stmtG->execute([$printData['student_id']]);
        $printStudentGrades = $stmtG->fetchAll();
    }
}
?>
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Document Processing &amp; Issuance - Registrar</title>
  <link rel="stylesheet" href="dashboard.css">
</head>
<body class="dashboard-body">

  <?php render_sidebar('document_processing.php'); ?>

  <div class="dash-main">
    <?php render_topbar($pdo, 'Document Processing'); ?>

    <div class="dash-content">
      <?php if ($flash): ?>
        <div class="alert-banner alert-<?php echo htmlspecialchars($flash['type']); ?>">
          <span><?php echo htmlspecialchars($flash['message']); ?></span>
        </div>
      <?php endif; ?>

      <div class="dash-heading-row">
        <div>
          <h1 class="dash-heading">📄 Official Document Processing &amp; Issuance</h1>
          <p class="dash-subheading">Process student certifications, transcript orders, diploma releases, and security QR generation.</p>
        </div>
        <div>
          <?php if ($tab !== 'issue'): ?>
            <a href="document_processing.php?tab=issue" class="btn btn-primary">➕ Issue New Request</a>
          <?php endif; ?>
        </div>
      </div>

      <!-- Tab Navigation -->
      <div class="tab-nav no-print">
        <a href="document_processing.php?tab=queue" class="tab-link <?php echo $tab === 'queue' ? 'active' : ''; ?>">
          <span>📋 Processing Queue (<?php echo count($requestsQueue); ?>)</span>
        </a>
        <a href="document_processing.php?tab=issue" class="tab-link <?php echo $tab === 'issue' ? 'active' : ''; ?>">
          <span>📝 Log / Issue Document Request</span>
        </a>
        <?php if ($printData): ?>
          <a href="document_processing.php?tab=print_doc&req_id=<?php echo $printReqId; ?>" class="tab-link active">
            <span>🖨️ Document Print Preview</span>
          </a>
        <?php endif; ?>
      </div>

      <!-- TAB 1: PROCESSING QUEUE -->
      <?php if ($tab === 'queue'): ?>
        <div class="dash-panel">
          <div class="dash-panel-header">
            <h2>Official Document Requests Queue</h2>
            <form method="get" style="display:flex;gap:8px;">
              <input type="hidden" name="tab" value="queue">
              <select name="status_filter" onchange="this.form.submit()" style="padding:4px 8px;font-size:12px;">
                <option value="">All Statuses</option>
                <option value="pending" <?php echo $statusFilter === 'pending' ? 'selected' : ''; ?>>Pending</option>
                <option value="processing" <?php echo $statusFilter === 'processing' ? 'selected' : ''; ?>>Processing</option>
                <option value="ready" <?php echo $statusFilter === 'ready' ? 'selected' : ''; ?>>Ready for Pickup</option>
                <option value="released" <?php echo $statusFilter === 'released' ? 'selected' : ''; ?>>Released</option>
                <option value="rejected" <?php echo $statusFilter === 'rejected' ? 'selected' : ''; ?>>Rejected</option>
              </select>
            </form>
          </div>

          <?php if (empty($requestsQueue)): ?>
            <p style="text-align:center;padding:30px;color:#64748b;">No document requests found.</p>
          <?php else: ?>
            <div class="table-responsive">
              <table class="table-simple">
                <thead>
                  <tr>
                    <th>Tracking Token</th>
                    <th>Student Name</th>
                    <th>Requested Document</th>
                    <th>Purpose / Copies</th>
                    <th>Status</th>
                    <th>Requested Date</th>
                    <th style="text-align:right;">Action</th>
                  </tr>
                </thead>
                <tbody>
                  <?php foreach ($requestsQueue as $r): ?>
                  <tr>
                    <td><strong class="token-chip" style="color:var(--accent);"><?php echo htmlspecialchars($r['qr_code_token']); ?></strong></td>
                    <td>
                      <strong><?php echo htmlspecialchars($r['student_name']); ?></strong>
                      <div style="font-size:11px;color:#64748b;"><?php echo htmlspecialchars($r['student_id_number']); ?> &middot; <?php echo htmlspecialchars($r['program']); ?></div>
                    </td>
                    <td><strong style="color:var(--text-main);"><?php echo htmlspecialchars($r['document_type']); ?></strong></td>
                    <td>
                      <div style="font-size:12px;color:#475569;"><?php echo htmlspecialchars($r['purpose'] ?: 'Official requirement'); ?></div>
                      <div style="font-size:11px;color:#64748b;"><?php echo $r['copies']; ?> copy(ies)</div>
                    </td>
                    <td><span class="badge badge-<?php echo strtolower($r['status']); ?>"><?php echo htmlspecialchars(ucfirst($r['status'])); ?></span></td>
                    <td><?php echo date('M d, Y', strtotime($r['requested_at'])); ?></td>
                    <td style="text-align:right;">
                      <div style="display:inline-flex;gap:6px;">
                        <a href="document_processing.php?tab=print_doc&req_id=<?php echo $r['id']; ?>" class="btn-small" title="Print Document">🖨️ Print</a>
                        <button onclick="openStatusModal(<?php echo $r['id']; ?>, '<?php echo $r['status']; ?>', '<?php echo htmlspecialchars(addslashes($r['remarks'] ?? '')); ?>')" class="btn-small primary">Update</button>
                      </div>
                    </td>
                  </tr>
                  <?php endforeach; ?>
                </tbody>
              </table>
            </div>
          <?php endif; ?>
        </div>

      <!-- TAB 2: ISSUE NEW REQUEST -->
      <?php elseif ($tab === 'issue'): ?>
        <div class="dash-panel" style="max-width:680px;margin:0 auto;">
          <div class="dash-panel-header">
            <h2>Log Official Document Request</h2>
          </div>
          <form method="post">
            <input type="hidden" name="action" value="create_request">

            <div class="form-group" style="margin-bottom:14px;">
              <label>Select Student *</label>
              <select name="student_id" required>
                <option value="">-- Choose Enrolled Student or Alumni --</option>
                <?php foreach ($studentsList as $s): ?>
                  <option value="<?php echo $s['id']; ?>"><?php echo htmlspecialchars($s['name'] . ' (' . $s['student_id_number'] . ') — ' . $s['program']); ?></option>
                <?php endforeach; ?>
              </select>
            </div>

            <div class="form-grid">
              <div class="form-group">
                <label>Document Type *</label>
                <select name="document_type" required>
                  <option value="Official Transcript of Records (TOR)">Official Transcript of Records (TOR)</option>
                  <option value="Certificate of Good Moral Character">Certificate of Good Moral Character</option>
                  <option value="Certificate of Grades">Certificate of Grades</option>
                  <option value="Certificate of Enrollment">Certificate of Enrollment</option>
                  <option value="Honorable Dismissal / Transfer Credential">Honorable Dismissal / Transfer Credential</option>
                  <option value="Diploma (Duplicate Copy)">Diploma (Duplicate Copy)</option>
                  <option value="Certified True Copy (CTC)">Certified True Copy (CTC)</option>
                </select>
              </div>
              <div class="form-group">
                <label>Number of Copies</label>
                <input type="number" name="copies" value="1" min="1" max="10">
              </div>
            </div>

            <div class="form-group" style="margin-bottom:14px;">
              <label>Purpose of Request *</label>
              <input type="text" name="purpose" placeholder="e.g. Employment, Board Exam (PRC), Graduate School Admission, Scholarship" required>
            </div>

            <div class="form-group" style="margin-bottom:20px;">
              <label>Remarks / Special Processing Instructions</label>
              <textarea name="remarks" rows="2" placeholder="e.g. Rush processing approved; dry seal on every page"></textarea>
            </div>

            <div style="display:flex;justify-content:flex-end;gap:10px;">
              <a href="document_processing.php?tab=queue" class="btn btn-secondary">Cancel</a>
              <button type="submit" class="btn btn-primary">Submit Request &amp; Generate Token</button>
            </div>
          </form>
        </div>

      <!-- TAB 3: DOCUMENT PRINT PREVIEW -->
      <?php elseif ($tab === 'print_doc' && $printData): ?>
        <div style="margin-bottom:16px;" class="no-print">
          <button onclick="window.print()" class="btn btn-primary">🖨️ Print Document</button>
          <a href="document_processing.php?tab=queue" class="btn btn-secondary">‹ Back to Queue</a>
        </div>

        <div class="official-document-sheet">
          <div class="doc-header">
            <div class="doc-institution">METROPOLITAN STATE UNIVERSITY</div>
            <div class="doc-sub">Office of the University Registrar &middot; Manila, Philippines</div>
            <div class="doc-title"><?php echo strtoupper(htmlspecialchars($printData['document_type'])); ?></div>
            <div style="font-size:11px;color:#555;">SECURITY TOKEN: <?php echo htmlspecialchars($printData['qr_code_token']); ?></div>
          </div>

          <!-- If Document is Good Moral -->
          <?php if (str_contains($printData['document_type'], 'Good Moral')): ?>
            <div style="font-size:14px;line-height:1.8;padding:20px 0;">
              <p style="text-align:right;font-size:13px;margin-bottom:30px;">Date Issued: <?php echo date('F d, Y'); ?></p>
              
              <p style="font-size:16px;font-weight:bold;text-align:center;margin-bottom:30px;">TO WHOM IT MAY CONCERN:</p>

              <p style="text-indent:40px;">
                This is to certify that <strong><?php echo strtoupper(htmlspecialchars($printData['student_name'])); ?></strong> (Student ID: <strong><?php echo htmlspecialchars($printData['student_id_number']); ?></strong>) is a student in good standing pursuing the degree program <strong><?php echo htmlspecialchars($printData['program']); ?></strong> at Metropolitan State University.
              </p>

              <p style="text-indent:40px;">
                Records on file with this Office and the University Guidance &amp; Counseling Center show that the above-named student has demonstrated exemplary conduct and has not been subjected to any disciplinary action or violation of university rules and regulations.
              </p>

              <p style="text-indent:40px;">
                This certification is issued upon the request of the interested party for <strong><?php echo htmlspecialchars($printData['purpose']); ?></strong> and for whatever legal purpose it may serve.
              </p>
            </div>

          <!-- If Document is TOR or Certificate of Grades -->
          <?php else: ?>
            <div style="margin-bottom:20px;font-size:12px;">
              <table style="width:100%;border-collapse:collapse;margin-bottom:16px;">
                <tr>
                  <td style="width:15%;font-weight:bold;">Student Name:</td>
                  <td style="width:45%;"><?php echo htmlspecialchars($printData['student_name']); ?></td>
                  <td style="width:15%;font-weight:bold;">Student ID:</td>
                  <td style="width:25%;"><?php echo htmlspecialchars($printData['student_id_number']); ?></td>
                </tr>
                <tr>
                  <td style="font-weight:bold;">Degree Program:</td>
                  <td><?php echo htmlspecialchars($printData['program']); ?></td>
                  <td style="font-weight:bold;">Cumulative GWA:</td>
                  <td><strong><?php echo $printData['average_grade'] ? number_format((float)$printData['average_grade'], 2) : '1.25'; ?></strong></td>
                </tr>
              </table>

              <table style="width:100%;border-collapse:collapse;font-size:11px;">
                <thead>
                  <tr style="border-top:1px solid #000;border-bottom:1px solid #000;text-align:left;">
                    <th style="padding:5px;">Term</th>
                    <th style="padding:5px;">Course Code</th>
                    <th style="padding:5px;">Course Descriptive Title</th>
                    <th style="padding:5px;">Units</th>
                    <th style="padding:5px;">Grade</th>
                    <th style="padding:5px;">Remarks</th>
                  </tr>
                </thead>
                <tbody>
                  <?php foreach ($printStudentGrades as $pg): 
                    $grText = $pg['is_inc'] ? 'INC' : ($pg['grade'] !== null ? number_format((float)$pg['grade'], 2) : 'IP');
                  ?>
                  <tr style="border-bottom:1px solid #eee;">
                    <td style="padding:4px;"><?php echo htmlspecialchars($pg['school_year'] . ' ' . $pg['semester']); ?></td>
                    <td style="padding:4px;font-weight:bold;"><?php echo htmlspecialchars($pg['subject_code']); ?></td>
                    <td style="padding:4px;"><?php echo htmlspecialchars($pg['subject_name']); ?></td>
                    <td style="padding:4px;"><?php echo htmlspecialchars($pg['units']); ?></td>
                    <td style="padding:4px;font-weight:bold;"><?php echo $grText; ?></td>
                    <td style="padding:4px;"><?php echo htmlspecialchars($pg['remarks'] ?: 'Passed'); ?></td>
                  </tr>
                  <?php endforeach; ?>
                </tbody>
              </table>
            </div>
          <?php endif; ?>

          <!-- Official Signatories & Dry Seal -->
          <div class="doc-signatories">
            <div class="doc-sign-box">
              <div class="doc-seal-box">
                OFFICIAL UNIVERSITY<br>DRY SEAL AREA
              </div>
              <div style="font-size:10px;color:#777;margin-top:6px;">Not valid without dry seal</div>
            </div>

            <div class="doc-sign-box">
              <div style="font-family:monospace;font-size:11px;margin-bottom:20px;">
                [ QR SECURITY VERIFICATION ]<br>
                <?php echo htmlspecialchars($printData['qr_code_token']); ?>
              </div>
              <div class="doc-sign-line">
                DR. ROSALINDA M. SANTOS, Ed.D.<br>
                <span style="font-weight:normal;font-size:11px;">University Registrar</span>
              </div>
            </div>
          </div>
        </div>
      <?php endif; ?>

    </div>
  </div>

  <!-- Modal: Update Status -->
  <div id="statusModal" style="display:none;position:fixed;top:0;left:0;right:0;bottom:0;background:rgba(15,23,42,0.6);z-index:999;">
    <div style="max-width:440px;background:#fff;border-radius:12px;margin:80px auto;padding:24px;box-shadow:0 20px 25px -5px rgba(0,0,0,0.2);">
      <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px;">
        <h2 style="margin:0;font-size:18px;">Update Request Status</h2>
        <button onclick="document.getElementById('statusModal').style.display='none'" style="background:none;border:none;font-size:20px;cursor:pointer;">&times;</button>
      </div>
      <form method="post">
        <input type="hidden" name="action" value="update_status">
        <input type="hidden" name="request_id" id="modalReqId">

        <div class="form-group" style="margin-bottom:14px;">
          <label>Lifecycle Status *</label>
          <select name="status" id="modalStatusSelect" required>
            <option value="pending">Pending</option>
            <option value="processing">Processing</option>
            <option value="ready">Ready for Pickup</option>
            <option value="released">Released</option>
            <option value="rejected">Rejected</option>
          </select>
        </div>

        <div class="form-group" style="margin-bottom:20px;">
          <label>Registrar Remarks</label>
          <textarea name="remarks" id="modalRemarksText" rows="3" placeholder="e.g. Printed, sealed, and ready at Window 3"></textarea>
        </div>

        <div style="display:flex;justify-content:flex-end;gap:10px;">
          <button type="button" class="btn btn-secondary" onclick="document.getElementById('statusModal').style.display='none'">Cancel</button>
          <button type="submit" class="btn btn-primary">Update Lifecycle</button>
        </div>
      </form>
    </div>
  </div>

  <script>
    function openStatusModal(id, status, remarks) {
      document.getElementById('modalReqId').value = id;
      document.getElementById('modalStatusSelect').value = status;
      document.getElementById('modalRemarksText').value = remarks;
      document.getElementById('statusModal').style.display = 'block';
    }
  </script>

</body>
</html>