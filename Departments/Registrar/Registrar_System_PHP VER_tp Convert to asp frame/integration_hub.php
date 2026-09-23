<?php
require 'config.php';

$tokens = $pdo->query("SELECT * FROM api_tokens ORDER BY id ASC")->fetchAll();
$students = $pdo->query("SELECT id, student_id_number, name FROM `user` WHERE role = 'student' ORDER BY name ASC LIMIT 10")->fetchAll();
$offerings = $pdo->query("SELECT id, section_code, subject_id FROM class_offerings LIMIT 10")->fetchAll();
?>
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>API &amp; System Interoperability Hub - Registrar</title>
  <link rel="stylesheet" href="dashboard.css">
</head>
<body class="dashboard-body">

  <?php render_sidebar('integration_hub.php'); ?>

  <div class="dash-main">
    <?php render_topbar($pdo, 'Integration Hub'); ?>

    <div class="dash-content">
      <div class="dash-heading-row">
        <div>
          <h1 class="dash-heading">🔌 Interoperability &amp; External Integration Hub</h1>
          <p class="dash-subheading">Connect external Student Portals, Faculty LMS systems, and Guidance &amp; Counseling apps via flexible JSON REST APIs.</p>
        </div>
      </div>

      <!-- System Connectivity Cards -->
      <div class="dash-stats">
        <div class="dash-stat-card">
          <div class="dash-stat-top">
            <div class="dash-stat-label">Student Portal API</div>
            <div class="dash-stat-icon">📱</div>
          </div>
          <div class="dash-stat-value" style="font-size:18px;color:var(--success);">Connected &amp; Active</div>
          <div class="dash-stat-sub">Grades &middot; Schedule &middot; Document Filing</div>
        </div>

        <div class="dash-stat-card">
          <div class="dash-stat-top">
            <div class="dash-stat-label">Faculty / LMS Sync</div>
            <div class="dash-stat-icon">👨‍🏫</div>
          </div>
          <div class="dash-stat-value" style="font-size:18px;color:var(--success);">Connected &amp; Active</div>
          <div class="dash-stat-sub">Assigned Sections &middot; Grade Submission</div>
        </div>

        <div class="dash-stat-card">
          <div class="dash-stat-top">
            <div class="dash-stat-label">Guidance &amp; Counseling</div>
            <div class="dash-stat-icon">🤝</div>
          </div>
          <div class="dash-stat-value" style="font-size:18px;color:var(--success);">Connected &amp; Active</div>
          <div class="dash-stat-sub">At-Risk Alerts &middot; Good Moral Clearance</div>
        </div>
      </div>

      <!-- Live Interactive API Test Console -->
      <div class="dash-panel">
        <div class="dash-panel-header">
          <h2>⚡ Live API Request &amp; Response Console</h2>
        </div>
        <p style="font-size:13px;color:#64748b;margin-top:0;">
          Test API endpoints in real-time. Select a target system, choose an action, and run queries against live registrar data.
        </p>

        <div class="form-grid" style="align-items:flex-end;">
          <div class="form-group">
            <label>Target Subsystem API</label>
            <select id="apiSystemSelect" onchange="updateActionOptions()">
              <option value="student">Student Portal API (/api/student.php)</option>
              <option value="faculty">Faculty Portal API (/api/faculty.php)</option>
              <option value="guidance">Guidance &amp; Counseling API (/api/guidance.php)</option>
            </select>
          </div>

          <div class="form-group">
            <label>API Action Endpoint</label>
            <select id="apiActionSelect">
              <!-- populated dynamically by JS -->
            </select>
          </div>

          <div class="form-group">
            <label>Parameter (Student ID / Offering ID)</label>
            <input type="text" id="apiParamInput" value="<?php echo $students[0]['id'] ?? 5; ?>" placeholder="e.g. 5">
          </div>

          <div>
            <button type="button" class="btn btn-primary" onclick="executeApiTest()" style="height:38px;">🚀 Send Test Request</button>
          </div>
        </div>

        <div style="margin-top:16px;">
          <div style="font-size:12px;font-weight:700;color:#334155;margin-bottom:6px;">Target URL: <span id="targetUrlDisplay" style="font-family:monospace;color:var(--accent);"></span></div>
          <pre id="apiConsoleOutput" class="code-block" style="min-height:160px;max-height:360px;">// Click 'Send Test Request' to view real-time JSON response from the Registrar API</pre>
        </div>
      </div>

      <!-- Active Authentication Tokens -->
      <div class="dash-panel">
        <div class="dash-panel-header">
          <h2>🔐 Authorized System Access Tokens</h2>
        </div>
        <div class="table-responsive">
          <table class="table-simple">
            <thead>
              <tr>
                <th>Subsystem Name</th>
                <th>Access Token (Bearer / X-API-Token)</th>
                <th>Permissions</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              <?php foreach ($tokens as $t): ?>
              <tr>
                <td><strong><?php echo htmlspecialchars($t['system_name']); ?></strong></td>
                <td><span class="token-chip"><?php echo htmlspecialchars($t['token']); ?></span></td>
                <td><span style="font-family:monospace;font-size:11px;color:#475569;"><?php echo htmlspecialchars($t['permissions']); ?></span></td>
                <td><span class="badge badge-active">Active</span></td>
              </tr>
              <?php endforeach; ?>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Integration Guide & Code Samples -->
      <div class="dash-grid-equal">
        <div class="dash-panel">
          <div class="dash-panel-header">
            <h2>💻 Integration Code: JavaScript / Fetch</h2>
          </div>
          <pre class="code-block">// Example: Student Portal fetching grades
const studentId = 5;
fetch(`http://localhost/Registrar_System_to_be_ported/api/student.php?action=grades&student_id=${studentId}`)
  .then(res => res.json())
  .then(data => {
    console.log("Student Grades:", data.data);
  });</pre>
        </div>

        <div class="dash-panel">
          <div class="dash-panel-header">
            <h2>💻 Integration Code: PHP cURL</h2>
          </div>
          <pre class="code-block">// Example: Guidance System querying at-risk students
$url = "http://localhost/Registrar_System_to_be_ported/api/guidance.php?action=at_risk_students";
$response = file_get_contents($url);
$data = json_decode($response, true);

foreach ($data['data'] as $student) {
    echo "Alert: " . $student['name'] . " - " . $student['academic_status'] . "\n";
}</pre>
        </div>
      </div>

    </div>
  </div>

  <script>
    const actionsMap = {
      student: [
        { value: 'profile', text: 'profile (Student info & GWA)' },
        { value: 'grades', text: 'grades (Complete grades history)' },
        { value: 'schedule', text: 'schedule (Current enrolled classes)' }
      ],
      faculty: [
        { value: 'my_classes', text: 'my_classes (Assigned sections)' },
        { value: 'class_roster', text: 'class_roster (Enrolled section students)' }
      ],
      guidance: [
        { value: 'at_risk_students', text: 'at_risk_students (Probation / Failed)' },
        { value: 'student_status', text: 'student_status (Academic audit)' },
        { value: 'verify_good_moral', text: 'verify_good_moral (Clearance check)' }
      ]
    };

    function updateActionOptions() {
      const sys = document.getElementById('apiSystemSelect').value;
      const actSelect = document.getElementById('apiActionSelect');
      actSelect.innerHTML = '';
      actionsMap[sys].forEach(act => {
        const opt = document.createElement('option');
        opt.value = act.value;
        opt.text = act.text;
        actSelect.appendChild(opt);
      });
      updateUrlDisplay();
    }

    function updateUrlDisplay() {
      const sys = document.getElementById('apiSystemSelect').value;
      const act = document.getElementById('apiActionSelect').value;
      const param = document.getElementById('apiParamInput').value;
      let url = `api/${sys}.php?action=${act}`;
      if (sys === 'student' || act === 'student_status' || act === 'verify_good_moral') {
        url += `&student_id=${param}`;
      } else if (act === 'class_roster') {
        url += `&class_offering_id=${param}`;
      }
      document.getElementById('targetUrlDisplay').innerText = url;
      return url;
    }

    document.getElementById('apiSystemSelect').addEventListener('change', updateActionOptions);
    document.getElementById('apiActionSelect').addEventListener('change', updateUrlDisplay);
    document.getElementById('apiParamInput').addEventListener('input', updateUrlDisplay);

    function executeApiTest() {
      const url = updateUrlDisplay();
      const output = document.getElementById('apiConsoleOutput');
      output.innerText = 'Loading request...';

      fetch(url)
        .then(res => res.json())
        .then(json => {
          output.innerText = JSON.stringify(json, null, 2);
        })
        .catch(err => {
          output.innerText = 'Error: ' + err.message;
        });
    }

    // Initialize
    updateActionOptions();
  </script>

</body>
</html>
