<?php
require 'config.php';
session_unset();
session_destroy();
header('Location: admin_dashboard.php');
exit;