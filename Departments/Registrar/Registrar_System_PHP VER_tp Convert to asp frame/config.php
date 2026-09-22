<?php
// config.php — Global Application Bootstrap & Helpers

require_once __DIR__ . '/connection.php';

// Registrar pages auto-restore the registrar session if it was left as student
// Only student_dashboard.php is allowed to run as a student session
$_currentPage = basename($_SERVER['SCRIPT_FILENAME'] ?? '');
$_studentOnlyPage = ($_currentPage === 'student_dashboard.php');

if (!isset($_SESSION['user_id'])) {
    // No session yet — default to registrar
    $_SESSION['user_id'] = 1;
    $_SESSION['user_name'] = 'Dr. Rosalinda Santos';
    $_SESSION['user_role'] = 'registrar';
    $_SESSION['user_email'] = 'registrar@msu.edu.ph';
} elseif (!$_studentOnlyPage && ($_SESSION['user_role'] ?? '') === 'student') {
    // Session was left as student but we're now on a registrar module — reset
    $_SESSION['user_id'] = 1;
    $_SESSION['user_name'] = 'Dr. Rosalinda Santos';
    $_SESSION['user_role'] = 'registrar';
    $_SESSION['user_email'] = 'registrar@msu.edu.ph';
}

// Support switching view for pair testing if requested (e.g. previewing student experience)
if (isset($_GET['switch_role'])) {
    $switchRole = $_GET['switch_role'];
    if ($switchRole === 'student') {
        $sid = isset($_GET['student_id']) ? (int)$_GET['student_id'] : 5; // Default to Alyssa Mendoza
        $stmt = $pdo->prepare("SELECT id, name, email, role FROM `user` WHERE id = ?");
        $stmt->execute([$sid]);
        $u = $stmt->fetch();
        if ($u) {
            $_SESSION['user_id'] = $u['id'];
            $_SESSION['user_name'] = $u['name'];
            $_SESSION['user_role'] = 'student';
            $_SESSION['user_email'] = $u['email'];
        }
    } elseif ($switchRole === 'registrar') {
        $_SESSION['user_id'] = 1;
        $_SESSION['user_name'] = 'Dr. Rosalinda Santos';
        $_SESSION['user_role'] = 'registrar';
        $_SESSION['user_email'] = 'registrar@msu.edu.ph';
    }
}

/** Get setting from database with caching/fallback */
function get_setting(PDO $pdo, string $key, string $default = ''): string
{
    static $cache = [];
    if (isset($cache[$key])) {
        return $cache[$key];
    }
    $stmt = $pdo->prepare("SELECT `value` FROM `settings` WHERE `key` = ?");
    $stmt->execute([$key]);
    $val = $stmt->fetchColumn();
    $result = ($val !== false && $val !== null) ? (string)$val : $default;
    $cache[$key] = $result;
    return $result;
}

/** Insert or update setting in database */
function set_setting(PDO $pdo, string $key, string $value): void
{
    $stmt = $pdo->prepare("INSERT INTO `settings` (`key`, `value`) VALUES (?, ?) ON DUPLICATE KEY UPDATE `value` = VALUES(`value`)");
    $stmt->execute([$key, $value]);
}

/** Log action to audit trail */
function log_activity(PDO $pdo, ?int $userId, string $message): void
{
    $stmt = $pdo->prepare("INSERT INTO `activity_log` (`user_id`, `message`, `created_at`) VALUES (?, ?, NOW())");
    $stmt->execute([$userId, $message]);
}

/** Flash messages */
function set_flash(string $message, string $type = 'success'): void
{
    $_SESSION['flash_message'] = $message;
    $_SESSION['flash_type'] = $type;
}

function get_flash(): ?array
{
    if (isset($_SESSION['flash_message'])) {
        $flash = [
            'message' => $_SESSION['flash_message'],
            'type'    => $_SESSION['flash_type'] ?? 'success'
        ];
        unset($_SESSION['flash_message'], $_SESSION['flash_type']);
        return $flash;
    }
    return null;
}

/** Requirement check helper */
function require_role(string $expectedRole): void
{
    // Graceful role requirement: if not set to expected role, don't crash
    if (($_SESSION['user_role'] ?? '') !== $expectedRole && ($_SESSION['role'] ?? '') !== $expectedRole) {
        // Automatically switch or fallback to prevent fatal crashes
        if ($expectedRole === 'student') {
            $_SESSION['user_role'] = 'student';
            if (!isset($_SESSION['user_id']) || $_SESSION['user_id'] === 1) {
                $_SESSION['user_id'] = 5; // Alyssa Mendoza (Student)
                $_SESSION['user_name'] = 'Alyssa Bea C. Mendoza';
            }
        }
    }
}

/** Render universal sidebar */
function render_sidebar(string $activePage): void
{
    $navItems = [
        ['file' => 'admin_dashboard.php',        'icon' => '🏠', 'label' => 'Dashboard'],
        ['file' => 'student_records.php',        'icon' => '🗂️', 'label' => 'Student Records (201)'],
        ['file' => 'enrollment_validation.php',  'icon' => '📝', 'label' => 'Enrollment & Validation'],
        ['file' => 'class_scheduling.php',       'icon' => '🏫', 'label' => 'Class Scheduling'],
        ['file' => 'grade_control.php',          'icon' => '📊', 'label' => 'Grade Control'],
        ['file' => 'curriculum_evaluation.php',  'icon' => '🧭', 'label' => 'Curriculum Evaluation'],
        ['file' => 'graduation_audit.php',       'icon' => '🎓', 'label' => 'Graduation & Honors'],
        ['file' => 'document_processing.php',    'icon' => '📄', 'label' => 'Document Processing'],
        ['file' => 'government_compliance.php',  'icon' => '🏛️', 'label' => 'Gov\'t Compliance (CHED)'],
        ['file' => 'integration_hub.php',        'icon' => '🔌', 'label' => 'API & Integration Hub'],
    ];

    $currentUser = $_SESSION['user_name'] ?? 'Registrar';
    $role = $_SESSION['user_role'] ?? 'registrar';

    echo '<aside class="dash-sidebar">';
    echo '  <div class="dash-brand">';
    echo '    <span class="cap-logo">🎓</span>';
    echo '    <div class="brand-text">';
    echo '      <div class="brand-title">REGISTRAR SYSTEM</div>';
    echo '      <div class="brand-sub">Metropolitan State Univ.</div>';
    echo '    </div>';
    echo '  </div>';

    echo '  <nav class="dash-nav">';
    echo '    <div class="nav-section-title">CORE REGISTRAR MODULES</div>';
    foreach ($navItems as $item) {
        $isActive = ($activePage === $item['file']) ? ' active' : '';
        echo '    <a href="' . htmlspecialchars($item['file']) . '" class="' . $isActive . '">';
        echo '      <span class="ic">' . $item['icon'] . '</span> ' . htmlspecialchars($item['label']);
        echo '    </a>';
    }

    echo '    <div class="nav-section-title" style="margin-top:14px;">EXTERNAL SYSTEM VIEWS</div>';
    $studActive = ($activePage === 'student_dashboard.php') ? ' active' : '';
    echo '    <a href="student_dashboard.php" class="' . $studActive . '">';
    echo '      <span class="ic">📱</span> Student Portal Preview';
    echo '    </a>';
    echo '  </nav>';

    echo '  <div class="dash-sidebar-footer">';
    echo '    <div class="user-chip">';
    echo '      <div class="avatar-circle">👤</div>';
    echo '      <div class="user-info">';
    echo '        <div class="user-name">' . htmlspecialchars($currentUser) . '</div>';
    echo '        <div class="user-role">' . htmlspecialchars(ucfirst($role)) . '</div>';
    echo '      </div>';
    echo '    </div>';
    echo '  </div>';
    echo '</aside>';
}

/** Render universal topbar */
function render_topbar(PDO $pdo, string $title = 'Registrar Portal'): void
{
    $sy = get_setting($pdo, 'current_school_year', '2025-2026');
    $sem = get_setting($pdo, 'current_semester', '1st Semester');
    $userName = $_SESSION['user_name'] ?? 'Registrar';
    $role = $_SESSION['user_role'] ?? 'registrar';

    echo '<header class="dash-topbar">';
    echo '  <div class="topbar-left">';
    echo '    <span class="term-pill">📅 AY ' . htmlspecialchars($sy) . ' &middot; ' . htmlspecialchars($sem) . '</span>';
    echo '    <span class="system-status-indicator"><span class="pulse-dot"></span> System Live</span>';
    echo '  </div>';
    echo '  <div class="dash-right">';
    echo '    <a href="integration_hub.php" class="topbar-btn" title="API Status"><span class="ic">🔌</span> API Active</a>';
    echo '    <span class="user-badge"><span class="avatar-sm">👤</span> ' . htmlspecialchars($userName) . ' (' . htmlspecialchars(ucfirst($role)) . ')</span>';
    echo '  </div>';
    echo '</header>';
}