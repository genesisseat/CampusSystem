<?php
// api/faculty.php — REST API for External Faculty Portal & LMS (Moodle/Canvas) Sync
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
$sy  = get_setting($pdo, 'current_school_year', '2025-2026');
$sem = get_setting($pdo, 'current_semester', '1st Semester');
$encodingOpen = get_setting($pdo, 'grade_encoding_open', '1');

try {
    switch ($action) {
        // 1. Faculty Assigned Classes
        case 'my_classes':
            $facultyName = trim($_GET['faculty_name'] ?? '');
            $sql = "SELECT co.*, s.subject_code, s.subject_name, s.units
                    FROM class_offerings co
                    JOIN subjects s ON s.id = co.subject_id
                    WHERE co.school_year = ? AND co.semester = ?";
            $params = [$sy, $sem];
            if ($facultyName !== '') {
                $sql .= " AND co.instructor_name LIKE ?";
                $params[] = "%$facultyName%";
            }
            $sql .= " ORDER BY co.section_code ASC";
            $stmt = $pdo->prepare($sql);
            $stmt->execute($params);
            $classes = $stmt->fetchAll();

            echo json_encode([
                'status' => 'success',
                'term' => ['school_year' => $sy, 'semester' => $sem],
                'encoding_open' => ($encodingOpen === '1'),
                'data' => $classes
            ]);
            break;

        // 2. Class Roster with Grade Marks
        case 'class_roster':
            $offeringId = (int)($_GET['class_offering_id'] ?? 0);
            if ($offeringId === 0) {
                echo json_encode(['status' => 'error', 'message' => 'class_offering_id required']);
                exit;
            }

            $stmt = $pdo->prepare(
                "SELECT es.id as enrolled_subject_id, u.id as student_id, u.student_id_number, u.name, u.email,
                        sp.program, sp.year_level, g.grade, g.is_inc, g.status as grade_status
                 FROM enrolled_subjects es
                 JOIN enrollments e ON e.id = es.enrollment_id
                 JOIN `user` u ON u.id = e.student_id
                 LEFT JOIN student_profile sp ON sp.user_id = u.id
                 LEFT JOIN grades g ON g.enrolled_subject_id = es.id
                 WHERE es.class_offering_id = ? AND es.status = 'enrolled'
                 ORDER BY u.name ASC"
            );
            $stmt->execute([$offeringId]);
            $roster = $stmt->fetchAll();

            echo json_encode(['status' => 'success', 'class_offering_id' => $offeringId, 'data' => $roster]);
            break;

        // 3. Batch Submit Grades from Faculty Portal
        case 'submit_grades':
            if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
                echo json_encode(['status' => 'error', 'message' => 'POST method required']);
                exit;
            }

            if ($encodingOpen !== '1') {
                echo json_encode(['status' => 'error', 'message' => 'Grade encoding window is currently CLOSED by the Registrar']);
                exit;
            }

            $rawInput = file_get_contents('php://input');
            $data = json_decode($rawInput, true) ?: $_POST;
            $grades = $data['grades'] ?? []; // Array of ['enrolled_subject_id' => ..., 'grade' => ..., 'is_inc' => ...]
            $facultyId = (int)($data['faculty_id'] ?? 0);

            if (empty($grades) || !is_array($grades)) {
                echo json_encode(['status' => 'error', 'message' => 'grades array is required']);
                exit;
            }

            $pdo->beginTransaction();
            $updatedCount = 0;
            foreach ($grades as $g) {
                $enrolledSubId = (int)($g['enrolled_subject_id'] ?? 0);
                $isInc = !empty($g['is_inc']) ? 1 : 0;
                $val = isset($g['grade']) ? (float)$g['grade'] : null;

                if ($isInc) {
                    $gradeToStore = null;
                    $remarks = 'Incomplete (INC)';
                } elseif ($val !== null && $val >= 1.00 && $val <= 5.00) {
                    $gradeToStore = $val;
                    $remarks = ($val <= 3.00) ? 'Passed' : 'Failed';
                } else {
                    continue;
                }

                $check = $pdo->prepare("SELECT id, status FROM grades WHERE enrolled_subject_id = ?");
                $check->execute([$enrolledSubId]);
                $existing = $check->fetch();

                if ($existing) {
                    if ($existing['status'] === 'locked') continue;
                    $upd = $pdo->prepare("UPDATE grades SET grade = ?, is_inc = ?, status = 'submitted', remarks = ?, encoded_by = ?, encoded_at = NOW() WHERE id = ?");
                    $upd->execute([$gradeToStore, $isInc, $remarks, $facultyId ?: null, $existing['id']]);
                } else {
                    $ins = $pdo->prepare("INSERT INTO grades (enrolled_subject_id, grade, is_inc, status, remarks, encoded_by, encoded_at) VALUES (?, ?, ?, 'submitted', ?, ?, NOW())");
                    $ins->execute([$enrolledSubId, $gradeToStore, $isInc, $remarks, $facultyId ?: null]);
                }
                $updatedCount++;
            }
            $pdo->commit();

            echo json_encode([
                'status' => 'success',
                'message' => "Successfully submitted {$updatedCount} student grades to Registrar verification queue"
            ]);
            break;

        default:
            echo json_encode([
                'status' => 'error',
                'message' => 'Unknown action. Available actions: my_classes, class_roster, submit_grades'
            ]);
            break;
    }
} catch (Throwable $e) {
    http_response_code(500);
    echo json_encode(['status' => 'error', 'message' => $e->getMessage()]);
}
