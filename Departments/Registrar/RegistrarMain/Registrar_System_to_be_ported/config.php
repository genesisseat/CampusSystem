<?php
// config.php — shared bootstrap.
// Authentication has been removed from this app: there is no login page,
// no session-based role checks, and every page below is reachable directly.
// This file is kept only so existing `require 'config.php';` calls in the
// module pages keep working without touching each of them individually.

require_once __DIR__ . '/connection.php'; // gives us $pdo + starts the session