<?php
// setup_db.php — Initialize and populate registrar_db with realistic Philippine college data

$host = 'localhost';
$user = 'root';
$pass = '';

try {
    $pdo = new PDO("mysql:host={$host};charset=utf8mb4", $user, $pass, [
        PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
        PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
    ]);
    
    // Create database if not exists
    $pdo->exec("CREATE DATABASE IF NOT EXISTS `registrar_db` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");
    $pdo->exec("USE `registrar_db`");
    
    // Disable foreign key checks for clean setup
    $pdo->exec("SET FOREIGN_KEY_CHECKS = 0");
    
    $tables = [
        "DROP TABLE IF EXISTS `api_tokens`",
        "DROP TABLE IF EXISTS `nstp_serial_numbers`",
        "DROP TABLE IF EXISTS `ched_special_orders`",
        "DROP TABLE IF EXISTS `course_shifting_requests`",
        "DROP TABLE IF EXISTS `transferee_credited_subjects`",
        "DROP TABLE IF EXISTS `transcript_requests`",
        "DROP TABLE IF EXISTS `completion_revision_requests`",
        "DROP TABLE IF EXISTS `overload_waiver_requests`",
        "DROP TABLE IF EXISTS `add_drop_requests`",
        "DROP TABLE IF EXISTS `grades`",
        "DROP TABLE IF EXISTS `enrolled_subjects`",
        "DROP TABLE IF EXISTS `enrollments`",
        "DROP TABLE IF EXISTS `class_offerings`",
        "DROP TABLE IF EXISTS `subjects`",
        "DROP TABLE IF EXISTS `document_credentials`",
        "DROP TABLE IF EXISTS `student_profile`",
        "DROP TABLE IF EXISTS `user`",
        "DROP TABLE IF EXISTS `settings`",
        "DROP TABLE IF EXISTS `activity_log`",
        "DROP TABLE IF EXISTS `system_status`"
    ];
    foreach ($tables as $drop) {
        $pdo->exec($drop);
    }
    
    // 1. Settings table
    $pdo->exec("CREATE TABLE `settings` (
        `key` VARCHAR(50) PRIMARY KEY,
        `value` TEXT NOT NULL,
        `description` VARCHAR(255) NULL
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");

    // 2. User table
    $pdo->exec("CREATE TABLE `user` (
        `id` INT AUTO_INCREMENT PRIMARY KEY,
        `student_id_number` VARCHAR(30) UNIQUE NULL,
        `name` VARCHAR(150) NOT NULL,
        `email` VARCHAR(150) UNIQUE NOT NULL,
        `password` VARCHAR(255) NOT NULL,
        `role` ENUM('admin', 'registrar', 'student', 'faculty', 'guidance') NOT NULL DEFAULT 'student',
        `status` ENUM('active', 'inactive') NOT NULL DEFAULT 'active',
        `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");

    // 3. Student Profile table
    $pdo->exec("CREATE TABLE `student_profile` (
        `user_id` INT PRIMARY KEY,
        `program` VARCHAR(100) NOT NULL DEFAULT 'BS Information Technology',
        `year_level` VARCHAR(50) NOT NULL DEFAULT '1st Year',
        `curriculum_year` VARCHAR(20) NOT NULL DEFAULT '2023-2027',
        `average_grade` DECIMAL(4,2) NULL,
        `academic_status` ENUM('Good Standing', 'Probation', 'Graduating', 'Graduated', 'Disqualified') NOT NULL DEFAULT 'Good Standing',
        `enrollment_status` ENUM('Active', 'On-Leave', 'Dropped', 'Dismissed', 'Graduated') NOT NULL DEFAULT 'Active',
        `completed_units` INT NOT NULL DEFAULT 0,
        `units_remaining` INT NOT NULL DEFAULT 144,
        `guidance_clearance_status` ENUM('Cleared', 'Pending', 'Flagged') NOT NULL DEFAULT 'Cleared',
        `guidance_notes` TEXT NULL,
        `contact_number` VARCHAR(30) NULL,
        `address` TEXT NULL,
        `birth_date` DATE NULL,
        `gender` ENUM('Male', 'Female') NOT NULL DEFAULT 'Female',
        FOREIGN KEY (`user_id`) REFERENCES `user`(`id`) ON DELETE CASCADE
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");

    // 4. Document Credentials (201 File)
    $pdo->exec("CREATE TABLE `document_credentials` (
        `id` INT AUTO_INCREMENT PRIMARY KEY,
        `student_id` INT NOT NULL,
        `document_type` VARCHAR(100) NOT NULL,
        `status` ENUM('Missing', 'Submitted', 'Verified') NOT NULL DEFAULT 'Missing',
        `remarks` TEXT NULL,
        `file_path` VARCHAR(255) NULL,
        `submitted_at` DATETIME NULL,
        `verified_at` DATETIME NULL,
        UNIQUE KEY `stud_doc` (`student_id`, `document_type`),
        FOREIGN KEY (`student_id`) REFERENCES `user`(`id`) ON DELETE CASCADE
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");

    // 5. Subjects Catalog
    $pdo->exec("CREATE TABLE `subjects` (
        `id` INT AUTO_INCREMENT PRIMARY KEY,
        `subject_code` VARCHAR(30) UNIQUE NOT NULL,
        `subject_name` VARCHAR(150) NOT NULL,
        `units` DECIMAL(3,1) NOT NULL DEFAULT 3.0,
        `lec_hours` INT NOT NULL DEFAULT 3,
        `lab_hours` INT NOT NULL DEFAULT 0,
        `prerequisite_subject_id` INT NULL,
        `curriculum_program` VARCHAR(100) NOT NULL DEFAULT 'BSIT',
        `year_level` VARCHAR(20) NOT NULL DEFAULT '1st Year',
        `semester` VARCHAR(20) NOT NULL DEFAULT '1st Semester',
        FOREIGN KEY (`prerequisite_subject_id`) REFERENCES `subjects`(`id`) ON DELETE SET NULL
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");

    // 6. Class Offerings
    $pdo->exec("CREATE TABLE `class_offerings` (
        `id` INT AUTO_INCREMENT PRIMARY KEY,
        `subject_id` INT NOT NULL,
        `section_code` VARCHAR(50) NOT NULL,
        `school_year` VARCHAR(20) NOT NULL,
        `semester` VARCHAR(20) NOT NULL,
        `room` VARCHAR(50) NOT NULL,
        `days_of_week` VARCHAR(50) NOT NULL,
        `start_time` TIME NOT NULL,
        `end_time` TIME NOT NULL,
        `instructor_name` VARCHAR(100) NOT NULL DEFAULT 'Prof. TBD',
        `capacity` INT NOT NULL DEFAULT 40,
        `slots_taken` INT NOT NULL DEFAULT 0,
        `status` ENUM('open', 'closed', 'cancelled') NOT NULL DEFAULT 'open',
        FOREIGN KEY (`subject_id`) REFERENCES `subjects`(`id`) ON DELETE CASCADE
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");

    // 7. Enrollments
    $pdo->exec("CREATE TABLE `enrollments` (
        `id` INT AUTO_INCREMENT PRIMARY KEY,
        `student_id` INT NOT NULL,
        `school_year` VARCHAR(20) NOT NULL,
        `semester` VARCHAR(20) NOT NULL,
        `enrollment_type` ENUM('new', 'old', 'transferee', 'cross-enrollee', 'returnee') NOT NULL DEFAULT 'old',
        `status` ENUM('pending', 'active', 'rejected', 'cancelled') NOT NULL DEFAULT 'pending',
        `total_units` DECIMAL(4,1) NOT NULL DEFAULT 0,
        `enrolled_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
        FOREIGN KEY (`student_id`) REFERENCES `user`(`id`) ON DELETE CASCADE
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");

    // 8. Enrolled Subjects
    $pdo->exec("CREATE TABLE `enrolled_subjects` (
        `id` INT AUTO_INCREMENT PRIMARY KEY,
        `enrollment_id` INT NOT NULL,
        `class_offering_id` INT NOT NULL,
        `status` ENUM('enrolled', 'dropped', 'passed', 'failed', 'inc') NOT NULL DEFAULT 'enrolled',
        FOREIGN KEY (`enrollment_id`) REFERENCES `enrollments`(`id`) ON DELETE CASCADE,
        FOREIGN KEY (`class_offering_id`) REFERENCES `class_offerings`(`id`) ON DELETE CASCADE
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");

    // 9. Grades
    $pdo->exec("CREATE TABLE `grades` (
        `id` INT AUTO_INCREMENT PRIMARY KEY,
        `enrolled_subject_id` INT UNIQUE NOT NULL,
        `grade` DECIMAL(3,2) NULL,
        `is_inc` TINYINT(1) NOT NULL DEFAULT 0,
        `status` ENUM('draft', 'submitted', 'verified', 'locked') NOT NULL DEFAULT 'draft',
        `remarks` VARCHAR(100) NULL,
        `encoded_by` INT NULL,
        `encoded_at` DATETIME NULL,
        `verified_by` INT NULL,
        `verified_at` DATETIME NULL,
        `locked_at` DATETIME NULL,
        FOREIGN KEY (`enrolled_subject_id`) REFERENCES `enrolled_subjects`(`id`) ON DELETE CASCADE
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");

    // 10. Add / Drop Requests
    $pdo->exec("CREATE TABLE `add_drop_requests` (
        `id` INT AUTO_INCREMENT PRIMARY KEY,
        `enrollment_id` INT NOT NULL,
        `student_id` INT NOT NULL,
        `request_type` ENUM('add', 'drop', 'change') NOT NULL,
        `class_offering_id` INT NULL,
        `target_class_offering_id` INT NULL,
        `reason` TEXT NULL,
        `status` ENUM('pending', 'approved', 'rejected') NOT NULL DEFAULT 'pending',
        `requested_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
        `processed_at` DATETIME NULL,
        `processed_by` INT NULL,
        FOREIGN KEY (`student_id`) REFERENCES `user`(`id`) ON DELETE CASCADE
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");

    // 11. Overload & Waiver Requests
    $pdo->exec("CREATE TABLE `overload_waiver_requests` (
        `id` INT AUTO_INCREMENT PRIMARY KEY,
        `student_id` INT NOT NULL,
        `school_year` VARCHAR(20) NOT NULL,
        `semester` VARCHAR(20) NOT NULL,
        `request_type` ENUM('overload', 'waiver', 'cross_enrollment') NOT NULL,
        `requested_units` INT NOT NULL DEFAULT 0,
        `reason` TEXT NULL,
        `status` ENUM('pending', 'approved', 'rejected') NOT NULL DEFAULT 'pending',
        `requested_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
        `processed_at` DATETIME NULL,
        `processed_by` INT NULL,
        FOREIGN KEY (`student_id`) REFERENCES `user`(`id`) ON DELETE CASCADE
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");

    // 12. Grade Completion & Revision Requests
    $pdo->exec("CREATE TABLE `completion_revision_requests` (
        `id` INT AUTO_INCREMENT PRIMARY KEY,
        `grade_id` INT NOT NULL,
        `student_id` INT NOT NULL,
        `request_type` ENUM('completion', 'revision') NOT NULL,
        `requested_grade` DECIMAL(3,2) NOT NULL,
        `reason` TEXT NULL,
        `status` ENUM('pending', 'approved', 'rejected') NOT NULL DEFAULT 'pending',
        `requested_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
        `processed_at` DATETIME NULL,
        `processed_by` INT NULL,
        FOREIGN KEY (`grade_id`) REFERENCES `grades`(`id`) ON DELETE CASCADE
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");

    // 13. Transcript & Document Requests
    $pdo->exec("CREATE TABLE `transcript_requests` (
        `id` INT AUTO_INCREMENT PRIMARY KEY,
        `student_id` INT NOT NULL,
        `document_type` VARCHAR(100) NOT NULL,
        `purpose` TEXT NULL,
        `copies` INT NOT NULL DEFAULT 1,
        `status` ENUM('pending', 'processing', 'ready', 'released', 'rejected') NOT NULL DEFAULT 'pending',
        `qr_code_token` VARCHAR(64) UNIQUE NOT NULL,
        `requested_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
        `processed_at` DATETIME NULL,
        `released_at` DATETIME NULL,
        `remarks` TEXT NULL,
        FOREIGN KEY (`student_id`) REFERENCES `user`(`id`) ON DELETE CASCADE
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");

    // 14. Transferee Credited Subjects (Curriculum Evaluation)
    $pdo->exec("CREATE TABLE `transferee_credited_subjects` (
        `id` INT AUTO_INCREMENT PRIMARY KEY,
        `student_id` INT NOT NULL,
        `prev_school` VARCHAR(150) NOT NULL,
        `prev_subject_code` VARCHAR(50) NOT NULL,
        `prev_subject_title` VARCHAR(150) NOT NULL,
        `prev_units` DECIMAL(3,1) NOT NULL,
        `prev_grade` VARCHAR(20) NOT NULL,
        `credited_to_subject_id` INT NOT NULL,
        `status` ENUM('pending', 'approved', 'rejected') NOT NULL DEFAULT 'approved',
        `evaluated_by` VARCHAR(100) NOT NULL DEFAULT 'Registrar Evaluator',
        `evaluated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
        FOREIGN KEY (`student_id`) REFERENCES `user`(`id`) ON DELETE CASCADE,
        FOREIGN KEY (`credited_to_subject_id`) REFERENCES `subjects`(`id`) ON DELETE CASCADE
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");

    // 15. Course Shifting Requests
    $pdo->exec("CREATE TABLE `course_shifting_requests` (
        `id` INT AUTO_INCREMENT PRIMARY KEY,
        `student_id` INT NOT NULL,
        `from_program` VARCHAR(100) NOT NULL,
        `to_program` VARCHAR(100) NOT NULL,
        `reason` TEXT NULL,
        `status` ENUM('pending', 'approved', 'rejected') NOT NULL DEFAULT 'pending',
        `requested_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
        `approved_at` DATETIME NULL,
        `evaluated_by` VARCHAR(100) NULL,
        FOREIGN KEY (`student_id`) REFERENCES `user`(`id`) ON DELETE CASCADE
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");

    // 16. CHED Special Orders
    $pdo->exec("CREATE TABLE `ched_special_orders` (
        `id` INT AUTO_INCREMENT PRIMARY KEY,
        `student_id` INT NOT NULL,
        `so_number` VARCHAR(100) NOT NULL,
        `series_year` VARCHAR(20) NOT NULL,
        `program` VARCHAR(100) NOT NULL,
        `date_applied` DATE NOT NULL,
        `date_issued` DATE NULL,
        `status` ENUM('Applied', 'Issued', 'Under Review', 'Pending Requirements') NOT NULL DEFAULT 'Applied',
        `remarks` TEXT NULL,
        FOREIGN KEY (`student_id`) REFERENCES `user`(`id`) ON DELETE CASCADE
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");

    // 17. NSTP Serial Numbers
    $pdo->exec("CREATE TABLE `nstp_serial_numbers` (
        `id` INT AUTO_INCREMENT PRIMARY KEY,
        `student_id` INT NOT NULL,
        `nstp_component` ENUM('CWTS', 'ROTC', 'LTS') NOT NULL DEFAULT 'CWTS',
        `serial_number` VARCHAR(100) UNIQUE NOT NULL,
        `date_issued` DATE NOT NULL,
        `remarks` TEXT NULL,
        FOREIGN KEY (`student_id`) REFERENCES `user`(`id`) ON DELETE CASCADE
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");

    // 18. API Tokens for Interoperability (Student, Faculty, Guidance portals)
    $pdo->exec("CREATE TABLE `api_tokens` (
        `id` INT AUTO_INCREMENT PRIMARY KEY,
        `system_name` VARCHAR(100) NOT NULL,
        `token` VARCHAR(64) UNIQUE NOT NULL,
        `permissions` VARCHAR(255) NOT NULL,
        `is_active` TINYINT(1) NOT NULL DEFAULT 1,
        `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");

    // 19. Activity Log
    $pdo->exec("CREATE TABLE `activity_log` (
        `id` INT AUTO_INCREMENT PRIMARY KEY,
        `user_id` INT NULL,
        `message` TEXT NOT NULL,
        `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");

    // 20. System Status
    $pdo->exec("CREATE TABLE `system_status` (
        `id` INT AUTO_INCREMENT PRIMARY KEY,
        `service_name` VARCHAR(100) NOT NULL,
        `status` VARCHAR(50) NOT NULL
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");

    $pdo->exec("SET FOREIGN_KEY_CHECKS = 1");
    echo "Tables created successfully.\n";

    // ---- SEED DATA ----

    // Settings
    $settings = [
        ['current_school_year', '2025-2026', 'Active Academic School Year'],
        ['current_semester', '1st Semester', 'Active Academic Semester'],
        ['grade_encoding_open', '1', 'Flag whether grade encoding is open for faculty/registrar'],
        ['grade_encoding_deadline', '2025-10-30', 'Deadline for grade encoding'],
        ['school_name', 'Metropolitan State University', 'Official School/University Name'],
        ['registrar_name', 'Dr. Rosalinda M. Santos, Ed.D', 'University Registrar Head'],
        ['registrar_email', 'registrar@msu.edu.ph', 'Official Registrar Email'],
        ['school_code', 'MSU-0422', 'CHED Institutional Code'],
        ['school_address', 'Academic Hub, Taft Avenue, Manila, Philippines', 'Institution Address']
    ];
    $stmt = $pdo->prepare("INSERT INTO `settings` (`key`, `value`, `description`) VALUES (?, ?, ?)");
    foreach ($settings as $s) {
        $stmt->execute($s);
    }

    // System Status
    $statusData = [
        ['Database Node', 'Operational'],
        ['Enrollment Engine', 'Online'],
        ['CHED Registry Link', 'Sync Active'],
        ['Grade Encoding Window', 'Open'],
        ['Guidance Interop API', 'Ready'],
        ['Faculty Grade Sync', 'Connected']
    ];
    $stmt = $pdo->prepare("INSERT INTO `system_status` (`service_name`, `status`) VALUES (?, ?)");
    foreach ($statusData as $st) {
        $stmt->execute($st);
    }

    // API Tokens for Interoperability
    $tokens = [
        ['Student Portal Mobile/Web', 'stu_live_token_77a912e8b4039f', 'student_read,document_request'],
        ['Faculty Portal LMS Sync', 'fac_live_token_88c4210d6e112a', 'faculty_read,grades_write,class_roster'],
        ['Guidance & Counseling System', 'gui_live_token_99f338a11b554c', 'guidance_read,clearance_write,risk_audit']
    ];
    $stmt = $pdo->prepare("INSERT INTO `api_tokens` (`system_name`, `token`, `permissions`) VALUES (?, ?, ?)");
    foreach ($tokens as $t) {
        $stmt->execute($t);
    }

    // Passwords hash
    $pwd = password_hash('password123', PASSWORD_DEFAULT);

    // Users (Registrar Admin, Faculty, Guidance, and 12 Students)
    $users = [
        // Staff
        ['REG-2020-001', 'Dr. Rosalinda Santos', 'registrar@msu.edu.ph', $pwd, 'registrar'],
        ['FAC-2018-045', 'Engr. Danilo Castillo', 'danilo.castillo@msu.edu.ph', $pwd, 'faculty'],
        ['FAC-2019-012', 'Prof. Maria Victoria Cruz', 'maria.cruz@msu.edu.ph', $pwd, 'faculty'],
        ['GUI-2021-008', 'Ma. Lourdes Ramos, RGC', 'guidance@msu.edu.ph', $pwd, 'guidance'],
        
        // Students (Various programs, years, standings)
        ['2022-00101', 'Alyssa Bea C. Mendoza', 'alyssa.mendoza@student.msu.edu.ph', $pwd, 'student'],
        ['2022-00102', 'Joshua Ryan T. Fernandez', 'joshua.fernandez@student.msu.edu.ph', $pwd, 'student'],
        ['2023-00201', 'Kirsten Nicole G. Reyes', 'kirsten.reyes@student.msu.edu.ph', $pwd, 'student'],
        ['2023-00202', 'Carl Christian B. Del Rosario', 'carl.delrosario@student.msu.edu.ph', $pwd, 'student'],
        ['2024-00301', 'Samantha Chloe V. Alcantara', 'samantha.alcantara@student.msu.edu.ph', $pwd, 'student'],
        ['2024-00302', 'Angelo Miguel S. Bautista', 'angelo.bautista@student.msu.edu.ph', $pwd, 'student'],
        ['2025-00401', 'Patricia Mae D. Gutierrez', 'patricia.gutierrez@student.msu.edu.ph', $pwd, 'student'],
        ['2025-00402', 'John Gabriel E. Aquino', 'john.aquino@student.msu.edu.ph', $pwd, 'student'],
        ['2022-00103', 'Rochelle Ann P. Soriano', 'rochelle.soriano@student.msu.edu.ph', $pwd, 'student'], // Graduating candidate
        ['2022-00104', 'Mark Dave L. Villanueva', 'mark.villanueva@student.msu.edu.ph', $pwd, 'student'], // Graduating candidate
        ['2023-00205', 'Christian Paul Z. Tan', 'christian.tan@student.msu.edu.ph', $pwd, 'student'], // Transferee
        ['2024-00308', 'Jasmine Joyce R. Navarro', 'jasmine.navarro@student.msu.edu.ph', $pwd, 'student']  // Probation
    ];

    $stmtUser = $pdo->prepare("INSERT INTO `user` (`student_id_number`, `name`, `email`, `password`, `role`) VALUES (?, ?, ?, ?, ?)");
    foreach ($users as $u) {
        $stmtUser->execute($u);
    }

    // Get User IDs
    $studMap = $pdo->query("SELECT student_id_number, id, name FROM `user` WHERE role = 'student'")->fetchAll(PDO::FETCH_GROUP|PDO::FETCH_UNIQUE);

    // Student Profiles
    $profiles = [
        // Alyssa Mendoza - 4th year BSIT, Graduating, Magna Cum Laude contender
        '2022-00101' => ['BS Information Technology', '4th Year', '2022-2026', 1.28, 'Graduating', 'Active', 138, 6, 'Cleared', 'Outstanding academic record', '09171234567', 'Quezon City, Metro Manila', '2003-05-14', 'Female'],
        // Joshua Fernandez - 4th year BSCS, Graduating, Cum Laude contender
        '2022-00102' => ['BS Computer Science', '4th Year', '2022-2026', 1.48, 'Graduating', 'Active', 140, 4, 'Cleared', 'No disciplinary issues', '09187654321', 'Manila City', '2003-08-20', 'Male'],
        // Kirsten Reyes - 3rd year BSIT, Good Standing
        '2023-00201' => ['BS Information Technology', '3rd Year', '2023-2027', 1.65, 'Good Standing', 'Active', 96, 48, 'Cleared', 'Active officer in IT Society', '09201122334', 'Makati City', '2004-02-11', 'Female'],
        // Carl Del Rosario - 3rd year BSBA
        '2023-00202' => ['BS Business Administration', '3rd Year', '2023-2027', 1.82, 'Good Standing', 'Active', 90, 54, 'Cleared', 'Good standing', '09224455667', 'Pasig City', '2004-09-03', 'Male'],
        // Samantha Alcantara - 2nd year BSIT
        '2024-00301' => ['BS Information Technology', '2nd Year', '2024-2028', 1.70, 'Good Standing', 'Active', 52, 92, 'Cleared', 'Regular student', '09193334455', 'Taguig City', '2005-06-18', 'Female'],
        // Angelo Bautista - 2nd year BSCS
        '2024-00302' => ['BS Computer Science', '2nd Year', '2024-2028', 2.10, 'Good Standing', 'Active', 48, 96, 'Cleared', 'Regular student', '09278889900', 'Mandaluyong City', '2005-11-25', 'Male'],
        // Patricia Gutierrez - 1st year BSIT
        '2025-00401' => ['BS Information Technology', '1st Year', '2025-2029', 1.50, 'Good Standing', 'Active', 23, 121, 'Cleared', 'Freshman top ranker', '09156677889', 'San Juan City', '2006-03-30', 'Female'],
        // John Gabriel Aquino - 1st year BSBA
        '2025-00402' => ['BS Business Administration', '1st Year', '2025-2029', 1.95, 'Good Standing', 'Active', 21, 123, 'Cleared', 'Freshman', '09169998877', 'Marikina City', '2006-07-12', 'Male'],
        // Rochelle Soriano - 4th year BSIT, Summa Cum Laude candidate
        '2022-00103' => ['BS Information Technology', '4th Year', '2022-2026', 1.15, 'Graduating', 'Active', 142, 2, 'Cleared', 'Dean\'s Lister consistently', '09285551122', 'Caloocan City', '2003-01-19', 'Female'],
        // Mark Dave Villanueva - 4th year BSCS, Graduating
        '2022-00104' => ['BS Computer Science', '4th Year', '2022-2026', 1.88, 'Graduating', 'Active', 138, 6, 'Cleared', 'Eligible for graduation', '09172223344', 'Parañaque City', '2003-10-05', 'Male'],
        // Christian Paul Tan - 2nd year Transferee
        '2023-00205' => ['BS Information Technology', '2nd Year', '2024-2028', 2.05, 'Good Standing', 'Active', 45, 99, 'Cleared', 'Transferee from FEU Tech', '09237776655', 'Las Piñas City', '2004-12-08', 'Male'],
        // Jasmine Joyce Navarro - On Academic Probation
        '2024-00308' => ['BS Information Technology', '2nd Year', '2024-2028', 3.12, 'Probation', 'Active', 36, 108, 'Flagged', 'Referred to guidance due to multiple INC/failed grades', '09264443322', 'Valenzuela City', '2005-04-22', 'Female']
    ];

    $stmtProf = $pdo->prepare("INSERT INTO `student_profile` 
        (`user_id`, `program`, `year_level`, `curriculum_year`, `average_grade`, `academic_status`, `enrollment_status`, `completed_units`, `units_remaining`, `guidance_clearance_status`, `guidance_notes`, `contact_number`, `address`, `birth_date`, `gender`)
        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)");

    $vaultDocs = ['Form 137', 'Form 138', 'Birth Certificate', 'Good Moral', 'Transcript from Previous School', 'Medical Clearance'];
    $stmtVault = $pdo->prepare("INSERT INTO `document_credentials` (`student_id`, `document_type`, `status`, `submitted_at`, `verified_at`, `remarks`) VALUES (?, ?, ?, ?, ?, ?)");

    foreach ($studMap as $idNum => $sInfo) {
        $uId = $sInfo['id'];
        if (isset($profiles[$idNum])) {
            $p = $profiles[$idNum];
            $stmtProf->execute(array_merge([$uId], $p));
        }

        // Add 201 file credentials
        foreach ($vaultDocs as $index => $doc) {
            $status = 'Verified';
            $subAt = '2024-08-15 10:00:00';
            $verAt = '2024-08-20 14:30:00';
            $remarks = 'Official authenticated copy verified by registrar.';
            
            if ($idNum === '2025-00401' && $doc === 'Medical Clearance') {
                $status = 'Submitted';
                $verAt = null;
                $remarks = 'Awaiting university clinic doctor signature';
            } elseif ($idNum === '2024-00308' && ($doc === 'Good Moral' || $doc === 'Birth Certificate')) {
                $status = 'Missing';
                $subAt = null;
                $verAt = null;
                $remarks = 'Deficiency notice sent to student';
            } elseif ($idNum === '2023-00205' && $doc === 'Transcript from Previous School') {
                $status = 'Verified';
                $remarks = 'Official TOR with seal received directly from previous institution';
            } elseif ($doc === 'Transcript from Previous School' && $idNum !== '2023-00205') {
                // Regular freshmen don't need previous college TOR
                $status = 'Verified';
                $remarks = 'Not applicable (Direct High School Graduate entry)';
            }
            $stmtVault->execute([$uId, $doc, $status, $subAt, $verAt, $remarks]);
        }
    }

    // 5. Subjects Catalog
    $subjects = [
        // 1st Year, 1st Sem
        ['IT101', 'Introduction to Computing', 3.0, 3, 0, null, 'BSIT', '1st Year', '1st Semester'],
        ['IT102', 'Computer Programming 1 (Python)', 3.0, 2, 3, null, 'BSIT', '1st Year', '1st Semester'],
        ['GE101', 'Understanding the Self', 3.0, 3, 0, null, 'All', '1st Year', '1st Semester'],
        ['MATH101', 'Mathematics in the Modern World', 3.0, 3, 0, null, 'All', '1st Year', '1st Semester'],
        ['NSTP1', 'National Service Training Program 1', 3.0, 3, 0, null, 'All', '1st Year', '1st Semester'],
        ['PE1', 'Physical Fitness and Wellness', 2.0, 2, 0, null, 'All', '1st Year', '1st Semester'],

        // 1st Year, 2nd Sem
        ['IT103', 'Computer Programming 2 (Java & OOP)', 3.0, 2, 3, 2, 'BSIT', '1st Year', '2nd Semester'], // prereq IT102
        ['IT104', 'Data Structures and Algorithms', 3.0, 2, 3, 2, 'BSIT', '1st Year', '2nd Semester'],
        ['GE102', 'Purposive Communication', 3.0, 3, 0, null, 'All', '1st Year', '2nd Semester'],
        ['NSTP2', 'National Service Training Program 2', 3.0, 3, 0, 5, 'All', '1st Year', '2nd Semester'],
        ['PE2', 'Rhythmic Activities and Dance', 2.0, 2, 0, 6, 'All', '1st Year', '2nd Semester'],

        // 2nd Year, 1st Sem
        ['IT201', 'Information Management & Databases', 3.0, 2, 3, 8, 'BSIT', '2nd Year', '1st Semester'],
        ['IT202', 'Web Systems and Technologies 1', 3.0, 2, 3, 7, 'BSIT', '2nd Year', '1st Semester'],
        ['IT203', 'Discrete Mathematics for IT', 3.0, 3, 0, 4, 'BSIT', '2nd Year', '1st Semester'],
        ['GE103', 'The Contemporary World', 3.0, 3, 0, null, 'All', '2nd Year', '1st Semester'],

        // 2nd Year, 2nd Sem
        ['IT204', 'Advanced Database Systems', 3.0, 2, 3, 12, 'BSIT', '2nd Year', '2nd Semester'],
        ['IT205', 'Networking and Communications 1', 3.0, 2, 3, 1, 'BSIT', '2nd Year', '2nd Semester'],
        ['GE104', 'Ethics and Professional Conduct', 3.0, 3, 0, null, 'All', '2nd Year', '2nd Semester'],

        // 3rd Year, 1st Sem
        ['IT301', 'Systems Analysis and Design', 3.0, 3, 0, 12, 'BSIT', '3rd Year', '1st Semester'],
        ['IT302', 'Information Assurance & Security 1', 3.0, 2, 3, 16, 'BSIT', '3rd Year', '1st Semester'],
        ['IT303', 'Mobile Applications Development', 3.0, 2, 3, 13, 'BSIT', '3rd Year', '1st Semester'],

        // 3rd Year, 2nd Sem
        ['IT304', 'Software Engineering & Capstone 1', 3.0, 2, 3, 18, 'BSIT', '3rd Year', '2nd Semester'],
        ['IT305', 'Cloud Computing & System Administration', 3.0, 2, 3, 16, 'BSIT', '3rd Year', '2nd Semester'],

        // 4th Year, 1st Sem
        ['IT401', 'Capstone Project 2 (Implementation)', 3.0, 1, 6, 21, 'BSIT', '4th Year', '1st Semester'],
        ['IT402', 'IT Practicum / On-the-Job Training (486 hrs)', 6.0, 0, 18, null, 'BSIT', '4th Year', '1st Semester'],
        ['IT403', 'Seminars and Field Trips in Emerging Tech', 3.0, 3, 0, null, 'BSIT', '4th Year', '1st Semester']
    ];

    $stmtSub = $pdo->prepare("INSERT INTO `subjects` (`id`, `subject_code`, `subject_name`, `units`, `lec_hours`, `lab_hours`, `prerequisite_subject_id`, `curriculum_program`, `year_level`, `semester`) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)");
    foreach ($subjects as $idx => $sb) {
        $id = $idx + 1;
        $stmtSub->execute(array_merge([$id], $sb));
    }

    // 6. Class Offerings for Current Term (2025-2026 1st Semester)
    $offerings = [
        [1, 'BSIT-1A', '2025-2026', '1st Semester', 'CL-301', 'Mon,Wed', '08:00:00', '09:30:00', 'Prof. Maria Victoria Cruz', 40, 35, 'open'],
        [2, 'BSIT-1A', '2025-2026', '1st Semester', 'IT-LAB-1', 'Tue,Thu', '10:00:00', '12:30:00', 'Engr. Danilo Castillo', 35, 35, 'closed'],
        [3, 'BSIT-1A', '2025-2026', '1st Semester', 'RM-204', 'Fri', '08:00:00', '11:00:00', 'Prof. Roberto Ramos', 45, 38, 'open'],
        [4, 'BSIT-1A', '2025-2026', '1st Semester', 'RM-205', 'Mon,Wed', '13:00:00', '14:30:00', 'Dr. Elena Valenzuela', 40, 36, 'open'],
        [5, 'BSIT-1A', '2025-2026', '1st Semester', 'GYM-A', 'Sat', '08:00:00', '11:00:00', 'Coach Jeffrey Santos', 50, 42, 'open'],

        // 2nd Year Section BSIT-2A
        [12, 'BSIT-2A', '2025-2026', '1st Semester', 'IT-LAB-2', 'Mon,Wed', '10:00:00', '12:30:00', 'Engr. Danilo Castillo', 35, 30, 'open'],
        [13, 'BSIT-2A', '2025-2026', '1st Semester', 'IT-LAB-3', 'Tue,Thu', '13:00:00', '15:30:00', 'Prof. Maria Victoria Cruz', 35, 29, 'open'],
        [14, 'BSIT-2A', '2025-2026', '1st Semester', 'RM-302', 'Fri', '13:00:00', '16:00:00', 'Dr. Elena Valenzuela', 40, 28, 'open'],

        // 3rd Year Section BSIT-3A
        [18, 'BSIT-3A', '2025-2026', '1st Semester', 'CL-305', 'Mon,Wed', '14:00:00', '15:30:00', 'Engr. Danilo Castillo', 40, 26, 'open'],
        [19, 'BSIT-3A', '2025-2026', '1st Semester', 'IT-LAB-2', 'Tue,Thu', '08:00:00', '10:30:00', 'Prof. Allan Gomez', 35, 26, 'open'],
        [20, 'BSIT-3A', '2025-2026', '1st Semester', 'IT-LAB-1', 'Fri', '08:00:00', '13:00:00', 'Prof. Maria Victoria Cruz', 35, 25, 'open'],

        // 4th Year Section BSIT-4A
        [23, 'BSIT-4A', '2025-2026', '1st Semester', 'IT-RES-ROOM', 'Wed', '13:00:00', '17:00:00', 'Dr. Rosalinda Santos', 30, 22, 'open'],
        [24, 'BSIT-4A', '2025-2026', '1st Semester', 'OJT-COOR', 'Sat', '09:00:00', '12:00:00', 'Prof. Roberto Ramos', 40, 24, 'open'],
        [25, 'BSIT-4A', '2025-2026', '1st Semester', 'AVR-1', 'Fri', '14:00:00', '17:00:00', 'Dr. Rosalinda Santos', 50, 24, 'open']
    ];

    $stmtOff = $pdo->prepare("INSERT INTO `class_offerings` 
        (`subject_id`, `section_code`, `school_year`, `semester`, `room`, `days_of_week`, `start_time`, `end_time`, `instructor_name`, `capacity`, `slots_taken`, `status`)
        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)");
    foreach ($offerings as $off) {
        $stmtOff->execute($off);
    }

    // Enrollments
    $enrollmentData = [
        // Alyssa Mendoza - 4th year
        ['2022-00101', '2025-2026', '1st Semester', 'old', 'active', 12.0, '2025-08-10 09:15:00'],
        // Joshua Fernandez - 4th year
        ['2022-00102', '2025-2026', '1st Semester', 'old', 'active', 12.0, '2025-08-11 11:30:00'],
        // Kirsten Reyes - 3rd year
        ['2023-00201', '2025-2026', '1st Semester', 'old', 'active', 21.0, '2025-08-12 14:00:00'],
        // Patricia Gutierrez - 1st year (New Student)
        ['2025-00401', '2025-2026', '1st Semester', 'new', 'active', 20.0, '2025-08-14 08:30:00'],
        // Rochelle Soriano - 4th year
        ['2022-00103', '2025-2026', '1st Semester', 'old', 'active', 12.0, '2025-08-10 10:00:00'],
        // Christian Paul Tan - Transferee
        ['2023-00205', '2025-2026', '1st Semester', 'transferee', 'active', 18.0, '2025-08-15 13:00:00'],
        // Jasmine Navarro - Pending enrollment verification
        ['2024-00308', '2025-2026', '1st Semester', 'old', 'pending', 15.0, '2025-08-20 16:45:00'],
        // John Gabriel Aquino - Pending
        ['2025-00402', '2025-2026', '1st Semester', 'new', 'pending', 18.0, '2025-08-21 09:20:00']
    ];

    $stmtEnr = $pdo->prepare("INSERT INTO `enrollments` (`student_id`, `school_year`, `semester`, `enrollment_type`, `status`, `total_units`, `enrolled_at`) VALUES (?, ?, ?, ?, ?, ?, ?)");
    foreach ($enrollmentData as $ed) {
        $studId = $studMap[$ed[0]]['id'];
        $stmtEnr->execute([$studId, $ed[1], $ed[2], $ed[3], $ed[4], $ed[5], $ed[6]]);
    }

    // Connect enrolled subjects for Alyssa Mendoza (4th year)
    $alyssaEnrId = $pdo->query("SELECT id FROM enrollments WHERE student_id = " . $studMap['2022-00101']['id'])->fetchColumn();
    $offerings4th = $pdo->query("SELECT id FROM class_offerings WHERE section_code = 'BSIT-4A'")->fetchAll(PDO::FETCH_COLUMN);
    $stmtEnrSub = $pdo->prepare("INSERT INTO `enrolled_subjects` (`enrollment_id`, `class_offering_id`, `status`) VALUES (?, ?, 'enrolled')");
    foreach ($offerings4th as $offId) {
        $stmtEnrSub->execute([$alyssaEnrId, $offId]);
    }

    // Connect enrolled subjects for Patricia Gutierrez (1st year)
    $patriciaEnrId = $pdo->query("SELECT id FROM enrollments WHERE student_id = " . $studMap['2025-00401']['id'])->fetchColumn();
    $offerings1st = $pdo->query("SELECT id FROM class_offerings WHERE section_code = 'BSIT-1A'")->fetchAll(PDO::FETCH_COLUMN);
    foreach ($offerings1st as $offId) {
        $stmtEnrSub->execute([$patriciaEnrId, $offId]);
    }

    // Connect enrolled subjects for Rochelle Soriano (4th year)
    $rochelleEnrId = $pdo->query("SELECT id FROM enrollments WHERE student_id = " . $studMap['2022-00103']['id'])->fetchColumn();
    foreach ($offerings4th as $offId) {
        $stmtEnrSub->execute([$rochelleEnrId, $offId]);
    }

    // Connect enrolled subjects for Kirsten Reyes (3rd year)
    $kirstenEnrId = $pdo->query("SELECT id FROM enrollments WHERE student_id = " . $studMap['2023-00201']['id'])->fetchColumn();
    $offerings3rd = $pdo->query("SELECT id FROM class_offerings WHERE section_code = 'BSIT-3A'")->fetchAll(PDO::FETCH_COLUMN);
    foreach ($offerings3rd as $offId) {
        $stmtEnrSub->execute([$kirstenEnrId, $offId]);
    }

    // Grades for Alyssa (4th year subjects draft/verified)
    $stmtGrade = $pdo->prepare("INSERT INTO `grades` (`enrolled_subject_id`, `grade`, `is_inc`, `status`, `remarks`, `encoded_by`, `encoded_at`, `verified_by`, `verified_at`) VALUES (?, ?, ?, ?, ?, ?, NOW(), ?, NOW())");
    $enrSubs = $pdo->query("SELECT id FROM enrolled_subjects WHERE enrollment_id = {$alyssaEnrId}")->fetchAll(PDO::FETCH_COLUMN);
    if (!empty($enrSubs)) {
        $gradesList = [1.25, 1.00, 1.25];
        foreach ($enrSubs as $i => $esId) {
            $g = $gradesList[$i % count($gradesList)];
            $stmtGrade->execute([$esId, $g, 0, 'verified', 'Passed - Excellent', 1, 1]);
        }
    }

    // Grades for Rochelle (Consistently 1.00 - 1.25, Summa Cum Laude)
    $enrSubsRochelle = $pdo->query("SELECT id FROM enrolled_subjects WHERE enrollment_id = {$rochelleEnrId}")->fetchAll(PDO::FETCH_COLUMN);
    if (!empty($enrSubsRochelle)) {
        foreach ($enrSubsRochelle as $i => $esId) {
            $stmtGrade->execute([$esId, 1.00, 0, 'locked', 'Passed - Superior', 1, 1]);
        }
    }

    // 10. Add / Drop Requests
    $stmtAddDrop = $pdo->prepare("INSERT INTO `add_drop_requests` (`enrollment_id`, `student_id`, `request_type`, `class_offering_id`, `target_class_offering_id`, `reason`, `status`, `requested_at`) VALUES (?, ?, ?, ?, ?, ?, ?, ?)");
    $stmtAddDrop->execute([
        $kirstenEnrId,
        $studMap['2023-00201']['id'],
        'change',
        $offerings3rd[0],
        $offerings3rd[1],
        'Conflict with student organization executive meeting schedule on Wednesday afternoon.',
        'pending',
        '2025-08-25 10:15:00'
    ]);

    // 11. Overload / Waiver Requests
    $stmtOverload = $pdo->prepare("INSERT INTO `overload_waiver_requests` (`student_id`, `school_year`, `semester`, `request_type`, `requested_units`, `reason`, `status`, `requested_at`) VALUES (?, ?, ?, ?, ?, ?, ?, ?)");
    $stmtOverload->execute([
        $studMap['2022-00101']['id'],
        '2025-2026',
        '1st Semester',
        'overload',
        24,
        'Graduating student in final year; requesting 3-unit overload to complete general elective requirements.',
        'pending',
        '2025-08-26 14:20:00'
    ]);

    // 12. Completion / Revision Requests
    $stmtComp = $pdo->prepare("INSERT INTO `completion_revision_requests` (`grade_id`, `student_id`, `request_type`, `requested_grade`, `reason`, `status`, `requested_at`) VALUES (?, ?, ?, ?, ?, ?, ?)");
    $firstGradeId = $pdo->query("SELECT id FROM grades LIMIT 1")->fetchColumn();
    if ($firstGradeId) {
        $stmtComp->execute([
            $firstGradeId,
            $studMap['2022-00101']['id'],
            'revision',
            1.00,
            'Faculty computation adjustment: missed major project score was submitted and credited with approval of Department Chair.',
            'pending',
            '2025-08-27 16:30:00'
        ]);
    }

    // 13. Transcript & Document Requests
    $docRequests = [
        [$studMap['2022-00101']['id'], 'Official Transcript of Records (TOR)', 'Employment & Board Exam application at PRC', 2, 'processing', 'QR-TOR-2025-09812A', '2025-08-20 11:00:00'],
        [$studMap['2022-00103']['id'], 'Certificate of Good Moral Character', 'Scholarship & Graduate School Admission', 1, 'ready', 'QR-GMC-2025-11044B', '2025-08-22 09:30:00'],
        [$studMap['2023-00201']['id'], 'Certificate of Grades', 'Scholarship renewal for CHED Tulong Dunong', 1, 'released', 'QR-COG-2025-22415C', '2025-08-18 14:10:00'],
        [$studMap['2025-00401']['id'], 'Certificate of Enrollment', 'SSS Educational Benefit Dependent Verification', 1, 'pending', 'QR-COE-2025-33901D', '2025-08-28 08:45:00'],
        [$studMap['2022-00102']['id'], 'Diploma (Duplicate Copy)', 'Lost original diploma during typhoon relocation', 1, 'pending', 'QR-DIP-2025-44119E', '2025-08-29 15:20:00']
    ];
    $stmtDoc = $pdo->prepare("INSERT INTO `transcript_requests` (`student_id`, `document_type`, `purpose`, `copies`, `status`, `qr_code_token`, `requested_at`) VALUES (?, ?, ?, ?, ?, ?, ?)");
    foreach ($docRequests as $dr) {
        $stmtDoc->execute($dr);
    }

    // 14. Transferee Credited Subjects (Curriculum Evaluation)
    $stmtCred = $pdo->prepare("INSERT INTO `transferee_credited_subjects` 
        (`student_id`, `prev_school`, `prev_subject_code`, `prev_subject_title`, `prev_units`, `prev_grade`, `credited_to_subject_id`, `status`, `evaluated_by`, `evaluated_at`)
        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)");
    $stmtCred->execute([
        $studMap['2023-00205']['id'],
        'Far Eastern University - Tech',
        'CS101',
        'Computer Concepts and Logic Formulation',
        3.0,
        '1.50',
        1, // IT101
        'approved',
        'Dr. Rosalinda Santos',
        '2024-08-10 11:20:00'
    ]);
    $stmtCred->execute([
        $studMap['2023-00205']['id'],
        'Far Eastern University - Tech',
        'ENG101',
        'College English & Communication Arts',
        3.0,
        '1.75',
        9, // GE102
        'approved',
        'Dr. Rosalinda Santos',
        '2024-08-10 11:25:00'
    ]);

    // 15. Course Shifting Requests
    $stmtShift = $pdo->prepare("INSERT INTO `course_shifting_requests` (`student_id`, `from_program`, `to_program`, `reason`, `status`, `requested_at`) VALUES (?, ?, ?, ?, ?, ?)");
    $stmtShift->execute([
        $studMap['2024-00302']['id'],
        'BS Computer Science',
        'BS Information Technology',
        'Career interest alignment with web/cloud infrastructure and enterprise network systems.',
        'pending',
        '2025-08-24 13:45:00'
    ]);

    // 16. CHED Special Orders
    $soData = [
        [$studMap['2022-00103']['id'], 'SO (B) No. 04-2025-10492', '2025', 'BS Information Technology', '2025-06-15', '2025-08-01', 'Issued', 'Special Order successfully approved by CHED NCR'],
        [$studMap['2022-00101']['id'], 'SO (B) No. 04-2025-10493', '2025', 'BS Information Technology', '2025-07-20', null, 'Under Review', 'Submitted to CHED Regional Office for batch endorsement'],
        [$studMap['2022-00102']['id'], 'SO (B) No. 04-2025-10494', '2025', 'BS Computer Science', '2025-07-20', null, 'Applied', 'Candidate documents undergoing final registrar clearance']
    ];
    $stmtSo = $pdo->prepare("INSERT INTO `ched_special_orders` (`student_id`, `so_number`, `series_year`, `program`, `date_applied`, `date_issued`, `status`, `remarks`) VALUES (?, ?, ?, ?, ?, ?, ?, ?)");
    foreach ($soData as $so) {
        $stmtSo->execute($so);
    }

    // 17. NSTP Serial Numbers
    $nstpData = [
        [$studMap['2022-00101']['id'], 'CWTS', 'NSTP-CWTS-2023-NCR-09412', '2023-06-15', 'Official DND/CHED certified completion serial'],
        [$studMap['2022-00102']['id'], 'ROTC', 'NSTP-ROTC-2023-NCR-04192', '2023-06-15', 'Commissioned / Certified basic ROTC cadet graduate'],
        [$studMap['2022-00103']['id'], 'CWTS', 'NSTP-CWTS-2023-NCR-09415', '2023-06-15', 'Official DND/CHED certified completion serial'],
        [$studMap['2023-00201']['id'], 'CWTS', 'NSTP-CWTS-2024-NCR-11204', '2024-06-20', 'Official DND/CHED certified completion serial']
    ];
    $stmtNstp = $pdo->prepare("INSERT INTO `nstp_serial_numbers` (`student_id`, `nstp_component`, `serial_number`, `date_issued`, `remarks`) VALUES (?, ?, ?, ?, ?)");
    foreach ($nstpData as $ns) {
        $stmtNstp->execute($ns);
    }

    // 19. Activity Log
    $activities = [
        [1, 'System database initialized with college curriculum and multi-role records.'],
        [$studMap['2022-00101']['id'], 'Enrollment approved for AY 2025-2026 1st Semester by Registrar.'],
        [$studMap['2022-00103']['id'], 'CHED Special Order No. 04-2025-10492 issued for BSIT graduation.'],
        [$studMap['2023-00205']['id'], 'Transferee subject credits accredited (CS101, ENG101) by Registrar.'],
        [$studMap['2025-00401']['id'], 'New student 201 File created and verified.']
    ];
    $stmtAct = $pdo->prepare("INSERT INTO `activity_log` (`user_id`, `message`, `created_at`) VALUES (?, ?, NOW())");
    foreach ($activities as $act) {
        $stmtAct->execute($act);
    }

    echo "Seed data inserted successfully! Everything is ready for production registrar operations.\n";

} catch (PDOException $e) {
    echo "ERROR: " . $e->getMessage() . "\n";
}
