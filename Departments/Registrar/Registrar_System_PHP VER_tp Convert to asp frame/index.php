<?php
// index.php — Root Entrypoint redirecting to Executive Dashboard
require_once __DIR__ . '/config.php';

header('Location: admin_dashboard.php');
exit;
