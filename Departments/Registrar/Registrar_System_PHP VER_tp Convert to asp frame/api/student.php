<?php
// api/student.php — REST API for External Student Portal & Mobile Apps
header('Content-Type: application/json; charset=utf-8');
header('Access-Control-Allow-Origin: *');
header('Access-Control-Allow-Methods: GET, POST, OPTIONS');
header('Access-Control-Allow-Headers: Content-Type, Authorization, X-API-Token');

require_once __DIR__ . '/../connection.php';
require_once __DIR__ . '/../config.php';

if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') {
    http_response_code(200);
    exit;
}

$action = $_GET['action'] ?? ($_POST['action'] ?? '');
$studentId = isset($_GET['student_id']) ? (int)$_GET['student_id'] : (int)($_POST['student_id'] ?? 0);
$studentIdNumber = trim($_GET['student_id_number'] ?? ($_POST['student_id_number'] ?? ''));

// Resolve student ID if student_id_number provided
if ($studentId === 0 && $studentIdNumber !== '') {
    $stmt = $pdo->prepare("SELECT id FROM `user` WHERE student_id_number = ? AND role = 'student'");
    $stmt->execute([$studentIdNumber]);
    $studentId = (int)$stmt->fetchColumn();
}

try {
    switch ($action) {
        // 1. Student Academic Profile
        case 'profile':
            if ($studentId === 0) {
                echo json_encode(['status' => 'error', 'message' => 'student_id or student_id_number required']);
                exit;
            }
            $stmt = $pdo->prepare(
                "SELECT u.id, u.student_id_number, u.name, u.email, sp.*
                 FROM `user` u
                 JOIN student_profile sp ON sp.user_id = u.id
                 WHERE u.id = ?"
            );
            $stmt->execute([$studentId]);
            $profile = $stmt->fetch();
            if (!$profile) {
                echo json_encode(['status' => 'error', 'message' => 'Student record not found']);
                exit;
            }
            echo json_encode(['status' => 'success', 'data' => $profile]);
            break;

        // 2. Student Grades
        case 'grades':
            if ($studentId === 0) {
                echo json_encode(['status' => 'error', 'message' => 'student_id required']);
                exit;
            }
            $stmt = $pdo->prepare(
                "SELECT e.school_year, e.semester, s.subject_code, s.subject_name, s.units, co.section_code,
                        g.grade, g.is_inc, g.status as grade_status, g.remarks
                 FROM enrolled_subjects es
                 JOIN enrollments e ON e.id = es.enrollment_id
                 JOIN class_offerings co ON co.id = es.class_offering_id
                 JOIN subjects s ON s.id = co.subject_id
                 LEFT JOIN grades g ON g.enrolled_subject_id = es.id
                 WHERE e.student_id = ?
                 ORDER BY e.school_year DESC, e.semester DESC, s.subject_code ASC"
            );
            $stmt->execute([$studentId]);
            $grades = $stmt->fetchAll();
            echo json_encode(['status' => 'success', 'data' => $grades]);
            break;

        // 3. Student Current Schedule
        case 'schedule':
            if ($studentId === 0) {
                echo json_encode(['status' => 'error', 'message' => 'student_id required']);
                exit;
            }
            $sy = get_setting($pdo, 'current_school_year', '2025-2026');
            $sem = get_setting($pdo, 'current_semester', '1st Semester');

            $stmt = $pdo->prepare(
                "SELECT co.section_code, s.subject_code, s.subject_name, s.units, co.room,
                        co.days_of_week, co.start_time, co.end_time, co.instructor_name
                 FROM enrolled_subjects es
                 JOIN enrollments e ON e.id = es.enrollment_id
                 JOIN class_offerings co ON co.id = es.class_offering_id
                 JOIN subjects s ON s.id = co.subject_id
                 WHERE e.student_id = ? AND e.school_year = ? AND e.semester = ? AND es.status = 'enrolled'
                 ORDER BY co.start_time ASC"
            );
            $stmt->execute([$studentId, $sy, $sem]);
            $sched = $stmt->fetchAll();
            echo json_encode(['status' => 'success', 'term' => ['school_year' => $sy, 'semester' => $sem], 'data' => $sched]);
            break;

        // 4. Submit Document Request
        case 'request_document':
            if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
                echo json_encode(['status' => 'error', 'message' => 'POST method required']);
                exit;
            }
            $docType = trim($_POST['document_type'] ?? '');
            $purpose = trim($_POST['purpose'] ?? 'Official personal requirement');
            $copies  = max(1, (int)($_POST['copies'] ?? 1));

            if ($studentId === 0 || $docType === '') {
                echo json_encode(['status' => 'error', 'message' => 'student_id and document_type required']);
                exit;
            }

            $prefix = strtoupper(substr(preg_replace('/[^A-Za-z]/', '', $docType), 0, 3));
            $token = 'MSU-' . $prefix . '-' . date('Y') . '-' . strtoupper(bin2hex(random_bytes(3)));

            $stmt = $pdo->prepare(
                "INSERT INTO transcript_requests 
                 (student_id, document_type, purpose, copies, status, qr_code_token, requested_at)
                 VALUES (?, ?, ?, ?, 'pending', ?, NOW())"
            );
            $stmt->execute([$studentId, $docType, $purpose, $copies, $token]);
            $newReqId = (int)$pdo->lastInsertId();

            log_activity($pdo, $studentId, "Document request filed via Student Portal API: {$docType} (Token: {$token})");

            echo json_encode([
                'status' => 'success',
                'message' => 'Document request received and queued',
                'data' => [
                    'request_id'     => $newReqId,
                    'document_type'  => $docType,
                    'tracking_token' => $token,
                    'status'         => 'pending'
                ]
            ]);
            break;

        default:
            echo json_encode([
                'status' => 'error',
                'message' => 'Unknown action. Available actions: profile, grades, schedule, request_document'
            ]);
            break;
    }
} catch (Throwable $e) {
    http_response_code(500);
    echo json_encode(['status' => 'error', 'message' => $e->getMessage()]);
}
