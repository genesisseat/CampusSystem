<?php
// api/guidance.php — REST API for External Guidance & Counseling Office System
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
        // 1. Check Student Scholastic Standing for Guidance
        case 'student_status':
            if ($studentId === 0) {
                echo json_encode(['status' => 'error', 'message' => 'student_id or student_id_number required']);
                exit;
            }

            $stmt = $pdo->prepare(
                "SELECT u.id, u.student_id_number, u.name, u.email, sp.program, sp.year_level, sp.average_grade,
                        sp.academic_status, sp.enrollment_status, sp.guidance_clearance_status, sp.guidance_notes,
                        (SELECT COUNT(*) FROM enrolled_subjects es JOIN grades g ON g.enrolled_subject_id = es.id JOIN enrollments e ON e.id = es.enrollment_id WHERE e.student_id = u.id AND g.grade > 3.00) as failed_subjects_count,
                        (SELECT COUNT(*) FROM enrolled_subjects es JOIN grades g ON g.enrolled_subject_id = es.id JOIN enrollments e ON e.id = es.enrollment_id WHERE e.student_id = u.id AND g.is_inc = 1) as inc_subjects_count
                 FROM `user` u
                 JOIN student_profile sp ON sp.user_id = u.id
                 WHERE u.id = ?"
            );
            $stmt->execute([$studentId]);
            $student = $stmt->fetch();

            if (!$student) {
                echo json_encode(['status' => 'error', 'message' => 'Student not found']);
                exit;
            }

            echo json_encode(['status' => 'success', 'data' => $student]);
            break;

        // 2. Query At-Risk Students (Probation or Multiple Deficiencies)
        case 'at_risk_students':
            $stmt = $pdo->query(
                "SELECT u.id, u.student_id_number, u.name, sp.program, sp.year_level, sp.average_grade,
                        sp.academic_status, sp.guidance_clearance_status, sp.guidance_notes
                 FROM `user` u
                 JOIN student_profile sp ON sp.user_id = u.id
                 WHERE sp.academic_status = 'Probation' 
                    OR sp.average_grade > 2.75 
                    OR sp.guidance_clearance_status = 'Flagged'
                 ORDER BY sp.average_grade DESC, u.name ASC"
            );
            $atRisk = $stmt->fetchAll();

            echo json_encode(['status' => 'success', 'count' => count($atRisk), 'data' => $atRisk]);
            break;

        // 3. Verify Good Moral Clearance
        case 'verify_good_moral':
            if ($studentId === 0) {
                echo json_encode(['status' => 'error', 'message' => 'student_id required']);
                exit;
            }

            $stmt = $pdo->prepare(
                "SELECT u.name, u.student_id_number, sp.guidance_clearance_status, sp.guidance_notes,
                        (SELECT status FROM document_credentials WHERE student_id = u.id AND document_type = 'Good Moral') as admission_good_moral_status
                 FROM `user` u
                 JOIN student_profile sp ON sp.user_id = u.id
                 WHERE u.id = ?"
            );
            $stmt->execute([$studentId]);
            $res = $stmt->fetch();

            $isCleared = ($res && $res['guidance_clearance_status'] === 'Cleared');

            echo json_encode([
                'status' => 'success',
                'is_cleared' => $isCleared,
                'clearance_status' => $res['guidance_clearance_status'] ?? 'Unknown',
                'guidance_notes' => $res['guidance_notes'] ?? '',
                'admission_record' => $res['admission_good_moral_status'] ?? 'Missing'
            ]);
            break;

        // 4. Update Guidance Clearance & Counseling Tag
        case 'update_clearance':
            if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
                echo json_encode(['status' => 'error', 'message' => 'POST method required']);
                exit;
            }

            $rawInput = file_get_contents('php://input');
            $data = json_decode($rawInput, true) ?: $_POST;
            $sid = (int)($data['student_id'] ?? $studentId);
            $clearance = trim($data['clearance_status'] ?? 'Cleared'); // Cleared, Pending, Flagged
            $notes = trim($data['notes'] ?? '');

            if ($sid === 0) {
                echo json_encode(['status' => 'error', 'message' => 'student_id required']);
                exit;
            }

            if (!in_array($clearance, ['Cleared', 'Pending', 'Flagged'], true)) {
                echo json_encode(['status' => 'error', 'message' => 'Invalid clearance_status (must be Cleared, Pending, or Flagged)']);
                exit;
            }

            $stmt = $pdo->prepare("UPDATE student_profile SET guidance_clearance_status = ?, guidance_notes = ? WHERE user_id = ?");
            $stmt->execute([$clearance, $notes, $sid]);

            log_activity($pdo, $sid, "Guidance clearance updated to {$clearance} by Guidance Office API.");

            echo json_encode([
                'status' => 'success',
                'message' => 'Guidance clearance updated',
                'student_id' => $sid,
                'clearance_status' => $clearance
            ]);
            break;

        default:
            echo json_encode([
                'status' => 'error',
                'message' => 'Unknown action. Available actions: student_status, at_risk_students, verify_good_moral, update_clearance'
            ]);
            break;
    }
} catch (Throwable $e) {
    http_response_code(500);
    echo json_encode(['status' => 'error', 'message' => $e->getMessage()]);
}
