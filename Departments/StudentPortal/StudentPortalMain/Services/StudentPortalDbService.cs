using System.Security.Cryptography;
using Dapper;
using MySqlConnector;
using StudentPortalMain.Models;

namespace StudentPortalMain.Services;

public class StudentPortalDbService
{
    private readonly MySqlConnection _db;
    private readonly IHttpContextAccessor _ctx;

    public StudentPortalDbService(MySqlConnection db, IHttpContextAccessor ctx)
    {
        _db = db;
        _ctx = ctx;
    }

    public MySqlConnection Connection => _db;

    private ISession? Session => _ctx.HttpContext?.Session;

    public static readonly string[] VaultDocumentTypes = [
        "Form 137",
        "Form 138",
        "Birth Certificate",
        "Good Moral",
        "Transcript from Previous School",
        "Medical Clearance"
    ];

    // ─── Current Session Helpers ─────────────────────────────────────────────

    public int CurrentStudentId
    {
        get
        {
            var id = Session?.GetInt32("student_user_id");
            if (id.HasValue && id.Value > 0) return id.Value;

            // Default to Alyssa Bea Mendoza (first student seeded, or lookup first student)
            var defaultId = GetDefaultStudentIdSync();
            if (Session != null) Session.SetInt32("student_user_id", defaultId);
            return defaultId;
        }
        set
        {
            Session?.SetInt32("student_user_id", value);
        }
    }

    private int GetDefaultStudentIdSync()
    {
        try
        {
            var first = _db.QueryFirstOrDefault<int>(
                "SELECT id FROM `user` WHERE role = 'student' AND status = 'active' ORDER BY id ASC LIMIT 1");
            return first > 0 ? first : 5;
        }
        catch
        {
            return 5;
        }
    }

    public void SwitchStudent(int studentId)
    {
        CurrentStudentId = studentId;
    }

    public async Task<List<StudentUser>> GetAllStudentsAsync()
    {
        var users = await _db.QueryAsync<StudentUser>(
            "SELECT id, student_id_number AS StudentIdNumber, name, email, role, status, created_at AS CreatedAt " +
            "FROM `user` WHERE role = 'student' ORDER BY name ASC");
        return users.AsList();
    }

    public async Task<StudentUser?> GetStudentUserAsync(int studentId)
    {
        return await _db.QueryFirstOrDefaultAsync<StudentUser>(
            "SELECT id, student_id_number AS StudentIdNumber, name, email, role, status, created_at AS CreatedAt " +
            "FROM `user` WHERE id = @id", new { id = studentId });
    }

    // ─── Academic Term & Settings ────────────────────────────────────────────

    public async Task<string> GetSettingAsync(string key, string defaultValue = "")
    {
        var val = await _db.QueryFirstOrDefaultAsync<string>(
            "SELECT `value` FROM `settings` WHERE `key` = @key", new { key });
        return val ?? defaultValue;
    }

    public async Task<(string SchoolYear, string Semester)> GetActiveTermAsync()
    {
        var sy = await GetSettingAsync("current_school_year", "2025-2026");
        var sem = await GetSettingAsync("current_semester", "1st Semester");
        return (sy, sem);
    }

    public async Task LogActivityAsync(int? userId, string message)
    {
        try
        {
            await _db.ExecuteAsync(
                "INSERT INTO `activity_log` (`user_id`, `message`, `created_at`) VALUES (@userId, @message, NOW())",
                new { userId, message });
        }
        catch
        {
            // non-fatal
        }
    }

    // ─── Student Profile ─────────────────────────────────────────────────────

    public async Task<StudentProfileData> GetStudentProfileAsync(int studentId)
    {
        var profile = await _db.QueryFirstOrDefaultAsync<StudentProfileData>(
            "SELECT user_id AS UserId, program, year_level AS YearLevel, curriculum_year AS CurriculumYear, " +
            "average_grade AS AverageGrade, academic_status AS AcademicStatus, enrollment_status AS EnrollmentStatus, " +
            "completed_units AS CompletedUnits, units_remaining AS UnitsRemaining, " +
            "guidance_clearance_status AS GuidanceClearanceStatus, guidance_notes AS GuidanceNotes, " +
            "contact_number AS ContactNumber, address AS Address, birth_date AS BirthDate, gender AS Gender " +
            "FROM student_profile WHERE user_id = @studentId", new { studentId });

        return profile ?? new StudentProfileData { UserId = studentId };
    }

    public async Task UpdateContactInfoAsync(int studentId, string contactNumber, string address)
    {
        await _db.ExecuteAsync(
            "UPDATE student_profile SET contact_number = @contactNumber, address = @address WHERE user_id = @studentId",
            new { studentId, contactNumber, address });

        await LogActivityAsync(studentId, "Updated personal contact details via Student Portal");
    }

    // ─── Schedule & Classes ──────────────────────────────────────────────────

    public async Task<List<EnrolledClassItem>> GetEnrolledClassesAsync(int studentId, string? schoolYear = null, string? semester = null)
    {
        if (string.IsNullOrEmpty(schoolYear) || string.IsNullOrEmpty(semester))
        {
            var term = await GetActiveTermAsync();
            schoolYear = term.SchoolYear;
            semester = term.Semester;
        }

        var sql = @"
            SELECT 
                es.id AS EnrolledSubjectId,
                co.id AS ClassOfferingId,
                s.subject_code AS SubjectCode,
                s.subject_name AS SubjectName,
                s.units AS Units,
                s.lec_hours AS LecHours,
                s.lab_hours AS LabHours,
                co.section_code AS SectionCode,
                co.instructor_name AS InstructorName,
                co.room AS Room,
                co.days_of_week AS DaysOfWeek,
                co.start_time AS StartTime,
                co.end_time AS EndTime,
                e.school_year AS SchoolYear,
                e.semester AS Semester,
                es.status AS Status
            FROM enrolled_subjects es
            JOIN enrollments e ON e.id = es.enrollment_id
            JOIN class_offerings co ON co.id = es.class_offering_id
            JOIN subjects s ON s.id = co.subject_id
            WHERE e.student_id = @studentId
              AND e.school_year = @schoolYear
              AND e.semester = @semester
              AND es.status = 'enrolled'
            ORDER BY co.start_time ASC, s.subject_code ASC";

        var list = await _db.QueryAsync<EnrolledClassItem>(sql, new { studentId, schoolYear, semester });
        return list.AsList();
    }

    public async Task<List<EnrolledClassItem>> GetAllEnrolledClassesAsync(int studentId)
    {
        var sql = @"
            SELECT 
                es.id AS EnrolledSubjectId,
                co.id AS ClassOfferingId,
                s.subject_code AS SubjectCode,
                s.subject_name AS SubjectName,
                s.units AS Units,
                s.lec_hours AS LecHours,
                s.lab_hours AS LabHours,
                co.section_code AS SectionCode,
                co.instructor_name AS InstructorName,
                co.room AS Room,
                co.days_of_week AS DaysOfWeek,
                co.start_time AS StartTime,
                co.end_time AS EndTime,
                e.school_year AS SchoolYear,
                e.semester AS Semester,
                es.status AS Status
            FROM enrolled_subjects es
            JOIN enrollments e ON e.id = es.enrollment_id
            JOIN class_offerings co ON co.id = es.class_offering_id
            JOIN subjects s ON s.id = co.subject_id
            WHERE e.student_id = @studentId
            ORDER BY e.school_year DESC, e.semester DESC, co.start_time ASC";

        var list = await _db.QueryAsync<EnrolledClassItem>(sql, new { studentId });
        return list.AsList();
    }

    // ─── Grades & Scholastic Record ──────────────────────────────────────────

    public async Task<List<TermGradeGroup>> GetGradesGroupedByTermAsync(int studentId)
    {
        var sql = @"
            SELECT 
                COALESCE(g.id, 0) AS GradeId,
                es.id AS EnrolledSubjectId,
                s.subject_code AS SubjectCode,
                s.subject_name AS SubjectName,
                s.units AS Units,
                co.section_code AS SectionCode,
                e.school_year AS SchoolYear,
                e.semester AS Semester,
                g.grade AS Grade,
                COALESCE(g.is_inc, 0) AS IsInc,
                COALESCE(g.status, 'pending') AS GradeStatus,
                g.remarks AS Remarks
            FROM enrolled_subjects es
            JOIN enrollments e ON e.id = es.enrollment_id
            JOIN class_offerings co ON co.id = es.class_offering_id
            JOIN subjects s ON s.id = co.subject_id
            LEFT JOIN grades g ON g.enrolled_subject_id = es.id
            WHERE e.student_id = @studentId
            ORDER BY e.school_year DESC, e.semester DESC, s.subject_code ASC";

        var allGrades = (await _db.QueryAsync<StudentGradeItem>(sql, new { studentId })).AsList();

        var groups = allGrades
            .GroupBy(x => new { x.SchoolYear, x.Semester })
            .Select(g =>
            {
                var validNumeric = g.Where(x => x.Grade.HasValue && !x.IsInc).ToList();
                decimal? gpa = null;
                if (validNumeric.Any())
                {
                    var totalWeighted = validNumeric.Sum(x => x.Grade!.Value * x.Units);
                    var totalUnits = validNumeric.Sum(x => x.Units);
                    if (totalUnits > 0)
                    {
                        gpa = Math.Round(totalWeighted / totalUnits, 2);
                    }
                }

                return new TermGradeGroup
                {
                    SchoolYear = g.Key.SchoolYear,
                    Semester = g.Key.Semester,
                    Grades = g.ToList(),
                    TotalUnits = g.Sum(x => x.Units),
                    SemesterGpa = gpa
                };
            })
            .ToList();

        return groups;
    }

    public async Task<decimal?> CalculateCumulativeGwaAsync(int studentId)
    {
        var sql = @"
            SELECT g.grade AS Grade, s.units AS Units
            FROM grades g
            JOIN enrolled_subjects es ON es.id = g.enrolled_subject_id
            JOIN enrollments e ON e.id = es.enrollment_id
            JOIN class_offerings co ON co.id = es.class_offering_id
            JOIN subjects s ON s.id = co.subject_id
            WHERE e.student_id = @studentId
              AND g.grade IS NOT NULL
              AND g.is_inc = 0";

        var list = (await _db.QueryAsync<(decimal Grade, int Units)>(sql, new { studentId })).AsList();
        if (!list.Any()) return null;

        var totalWeighted = list.Sum(x => x.Grade * x.Units);
        var totalUnits = list.Sum(x => x.Units);
        return totalUnits > 0 ? Math.Round(totalWeighted / totalUnits, 2) : null;
    }

    // ─── Enrollment & COR ────────────────────────────────────────────────────

    public async Task<StudentEnrollmentOverview?> GetActiveEnrollmentOverviewAsync(int studentId, string? schoolYear = null, string? semester = null)
    {
        if (string.IsNullOrEmpty(schoolYear) || string.IsNullOrEmpty(semester))
        {
            var term = await GetActiveTermAsync();
            schoolYear = term.SchoolYear;
            semester = term.Semester;
        }

        var enrollment = await _db.QueryFirstOrDefaultAsync<StudentEnrollmentOverview>(
            @"SELECT id, student_id AS StudentId, school_year AS SchoolYear, semester AS Semester, 
                     enrollment_type AS EnrollmentType, status, enrolled_at AS EnrolledAt 
              FROM enrollments 
              WHERE student_id = @studentId AND school_year = @schoolYear AND semester = @semester 
              ORDER BY id DESC LIMIT 1",
            new { studentId, schoolYear, semester });

        if (enrollment == null)
        {
            // fallback: return most recent enrollment
            enrollment = await _db.QueryFirstOrDefaultAsync<StudentEnrollmentOverview>(
                @"SELECT id, student_id AS StudentId, school_year AS SchoolYear, semester AS Semester, 
                         enrollment_type AS EnrollmentType, status, enrolled_at AS EnrolledAt 
                  FROM enrollments 
                  WHERE student_id = @studentId 
                  ORDER BY id DESC LIMIT 1",
                new { studentId });
        }

        if (enrollment != null)
        {
            enrollment.Subjects = await GetEnrolledClassesAsync(studentId, enrollment.SchoolYear, enrollment.Semester);
            enrollment.TotalUnits = enrollment.Subjects.Sum(s => s.Units);
            enrollment.SubjectCount = enrollment.Subjects.Count;
        }

        return enrollment;
    }

    public async Task<List<StudentEnrollmentOverview>> GetEnrollmentHistoryAsync(int studentId)
    {
        var enrollments = (await _db.QueryAsync<StudentEnrollmentOverview>(
            @"SELECT id, student_id AS StudentId, school_year AS SchoolYear, semester AS Semester, 
                     enrollment_type AS EnrollmentType, status, enrolled_at AS EnrolledAt 
              FROM enrollments 
              WHERE student_id = @studentId 
              ORDER BY school_year DESC, semester DESC, id DESC",
            new { studentId })).AsList();

        foreach (var en in enrollments)
        {
            en.Subjects = await GetEnrolledClassesAsync(studentId, en.SchoolYear, en.Semester);
            en.TotalUnits = en.Subjects.Sum(s => s.Units);
            en.SubjectCount = en.Subjects.Count;
        }

        return enrollments;
    }

    // ─── Add/Drop & Overload Petitions ───────────────────────────────────────

    public async Task<List<AddDropRecord>> GetAddDropRequestsAsync(int studentId)
    {
        var sql = @"
            SELECT 
                adr.id,
                adr.student_id AS StudentId,
                adr.enrollment_id AS EnrollmentId,
                adr.request_type AS RequestType,
                adr.class_offering_id AS ClassOfferingId,
                s1.subject_code AS SubjectCode,
                co1.section_code AS SectionCode,
                adr.target_class_offering_id AS TargetClassOfferingId,
                s2.subject_code AS TargetSubjectCode,
                co2.section_code AS TargetSectionCode,
                adr.reason AS Reason,
                adr.status AS Status,
                adr.requested_at AS CreatedAt
            FROM add_drop_requests adr
            LEFT JOIN class_offerings co1 ON co1.id = adr.class_offering_id
            LEFT JOIN subjects s1 ON s1.id = co1.subject_id
            LEFT JOIN class_offerings co2 ON co2.id = adr.target_class_offering_id
            LEFT JOIN subjects s2 ON s2.id = co2.subject_id
            WHERE adr.student_id = @studentId
            ORDER BY adr.id DESC";

        var list = await _db.QueryAsync<AddDropRecord>(sql, new { studentId });
        return list.AsList();
    }

    public async Task<int> SubmitAddDropRequestAsync(int studentId, int enrollmentId, string requestType, int? classOfferingId, int? targetClassOfferingId, string reason)
    {
        var sql = @"
            INSERT INTO add_drop_requests 
            (student_id, enrollment_id, request_type, class_offering_id, target_class_offering_id, reason, status, requested_at)
            VALUES (@studentId, @enrollmentId, @requestType, @classOfferingId, @targetClassOfferingId, @reason, 'pending', NOW())";

        await _db.ExecuteAsync(sql, new { studentId, enrollmentId, requestType, classOfferingId, targetClassOfferingId, reason });
        var id = await _db.QueryFirstOrDefaultAsync<int>("SELECT LAST_INSERT_ID()");

        await LogActivityAsync(studentId, $"Filed {requestType.ToUpper()} petition (ID #{id}) via Student Portal — queued for Registrar validation");
        return id;
    }

    public async Task<List<OverloadRecord>> GetOverloadRequestsAsync(int studentId)
    {
        var sql = @"
            SELECT 
                id, student_id AS StudentId, school_year AS SchoolYear, semester AS Semester,
                request_type AS RequestType, requested_units AS RequestedUnits, reason AS Reason,
                status AS Status, requested_at AS RequestedAt
            FROM overload_waiver_requests
            WHERE student_id = @studentId
            ORDER BY id DESC";

        var list = await _db.QueryAsync<OverloadRecord>(sql, new { studentId });
        return list.AsList();
    }

    public async Task<int> SubmitOverloadRequestAsync(int studentId, string schoolYear, string semester, string requestType, int requestedUnits, string reason)
    {
        var sql = @"
            INSERT INTO overload_waiver_requests
            (student_id, school_year, semester, request_type, requested_units, reason, status, requested_at)
            VALUES (@studentId, @schoolYear, @semester, @requestType, @requestedUnits, @reason, 'pending', NOW())";

        await _db.ExecuteAsync(sql, new { studentId, schoolYear, semester, requestType, requestedUnits, reason });
        var id = await _db.QueryFirstOrDefaultAsync<int>("SELECT LAST_INSERT_ID()");

        await LogActivityAsync(studentId, $"Filed {requestType.ToUpper()} petition for {requestedUnits} units ({schoolYear} {semester}) via Student Portal");
        return id;
    }

    // ─── Document Requests (Registrar Transcript Requests) ───────────────────

    public async Task<List<DocumentRequestRecord>> GetDocumentRequestsAsync(int studentId)
    {
        var sql = @"
            SELECT 
                id, student_id AS StudentId, document_type AS DocumentType, purpose AS Purpose,
                copies AS Copies, status AS Status, qr_code_token AS QrCodeToken, remarks AS Remarks,
                requested_at AS RequestedAt, processed_at AS ProcessedAt, released_at AS ReleasedAt
            FROM transcript_requests
            WHERE student_id = @studentId
            ORDER BY requested_at DESC, id DESC";

        var list = await _db.QueryAsync<DocumentRequestRecord>(sql, new { studentId });
        return list.AsList();
    }

    public async Task<string> SubmitDocumentRequestAsync(int studentId, string documentType, string purpose, int copies)
    {
        var prefix = new string(documentType.Where(char.IsLetter).Take(3).ToArray()).ToUpper();
        if (string.IsNullOrEmpty(prefix)) prefix = "DOC";
        var randomHex = Convert.ToHexString(RandomNumberGenerator.GetBytes(3));
        var token = $"MSU-{prefix}-{DateTime.UtcNow.Year}-{randomHex}";

        var sql = @"
            INSERT INTO transcript_requests 
            (student_id, document_type, purpose, copies, status, qr_code_token, requested_at)
            VALUES (@studentId, @documentType, @purpose, @copies, 'pending', @token, NOW())";

        await _db.ExecuteAsync(sql, new { studentId, documentType, purpose, copies, token });

        await LogActivityAsync(studentId, $"Filed Document Request ({documentType}, {copies} copy/ies) - Token: {token} via Student Portal");
        return token;
    }

    // ─── 201 File Document Vault ─────────────────────────────────────────────

    public async Task EnsureVaultRowsAsync(int studentId)
    {
        foreach (var type in VaultDocumentTypes)
        {
            await _db.ExecuteAsync(
                "INSERT IGNORE INTO document_credentials (student_id, document_type) VALUES (@studentId, @type)",
                new { studentId, type });
        }
    }

    public async Task<List<DocumentCredentialItem>> GetDocumentVaultAsync(int studentId)
    {
        await EnsureVaultRowsAsync(studentId);

        var sql = @"
            SELECT id, student_id AS StudentId, document_type AS DocumentType, 
                   status, remarks, file_path AS FilePath, 
                   submitted_at AS SubmittedAt, verified_at AS VerifiedAt 
            FROM document_credentials 
            WHERE student_id = @studentId 
            ORDER BY id ASC";

        var list = await _db.QueryAsync<DocumentCredentialItem>(sql, new { studentId });
        return list.AsList();
    }

    public async Task SubmitVaultFileAsync(int credentialId, int studentId, string fileName)
    {
        await _db.ExecuteAsync(
            "UPDATE document_credentials SET status = 'Submitted', file_path = @fileName, submitted_at = NOW() " +
            "WHERE id = @credentialId AND student_id = @studentId",
            new { credentialId, studentId, fileName });

        await LogActivityAsync(studentId, $"Uploaded 201 credential file ({fileName}) for Registrar verification");
    }

    // ─── Available Class Offerings for Search & Add/Drop ─────────────────────

    public async Task<List<EnrolledClassItem>> GetAvailableOfferingsAsync(string? schoolYear = null, string? semester = null)
    {
        if (string.IsNullOrEmpty(schoolYear) || string.IsNullOrEmpty(semester))
        {
            var term = await GetActiveTermAsync();
            schoolYear = term.SchoolYear;
            semester = term.Semester;
        }

        var sql = @"
            SELECT 
                0 AS EnrolledSubjectId,
                co.id AS ClassOfferingId,
                s.subject_code AS SubjectCode,
                s.subject_name AS SubjectName,
                s.units AS Units,
                s.lec_hours AS LecHours,
                s.lab_hours AS LabHours,
                co.section_code AS SectionCode,
                co.instructor_name AS InstructorName,
                co.room AS Room,
                co.days_of_week AS DaysOfWeek,
                co.start_time AS StartTime,
                co.end_time AS EndTime,
                co.school_year AS SchoolYear,
                co.semester AS Semester,
                co.status AS Status
            FROM class_offerings co
            JOIN subjects s ON s.id = co.subject_id
            WHERE co.school_year = @schoolYear AND co.semester = @semester
            ORDER BY s.subject_code ASC, co.section_code ASC";

        var list = await _db.QueryAsync<EnrolledClassItem>(sql, new { schoolYear, semester });
        return list.AsList();
    }

    // ─── Course Shifting Petitions (Registrar Flow) ──────────────────────────

    public async Task<List<CourseShiftingRecord>> GetCourseShiftingRequestsAsync(int studentId)
    {
        var sql = @"
            SELECT id, student_id AS StudentId, from_program AS FromProgram, 
                   to_program AS ToProgram, reason AS Reason, status, 
                   requested_at AS RequestedAt, approved_at AS ApprovedAt, 
                   evaluated_by AS EvaluatedBy 
            FROM course_shifting_requests 
            WHERE student_id = @studentId 
            ORDER BY requested_at DESC, id DESC";

        var list = await _db.QueryAsync<CourseShiftingRecord>(sql, new { studentId });
        return list.AsList();
    }

    public async Task<int> SubmitCourseShiftingRequestAsync(int studentId, string fromProg, string toProg, string reason)
    {
        var sql = @"
            INSERT INTO course_shifting_requests 
            (student_id, from_program, to_program, reason, status, requested_at) 
            VALUES (@studentId, @fromProg, @toProg, @reason, 'pending', NOW())";

        await _db.ExecuteAsync(sql, new { studentId, fromProg, toProg, reason });
        var id = await _db.QueryFirstOrDefaultAsync<int>("SELECT LAST_INSERT_ID()");

        await LogActivityAsync(studentId, $"Submitted Program Shifting Petition ({fromProg} -> {toProg}) via Student Portal");
        return id;
    }

    // ─── Semestral Clearance (Registrar, Guidance, Library, Finance Flow) ────

    public async Task EnsureClearanceRowsAsync(int studentId, string schoolYear, string semester)
    {
        try
        {
            await _db.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS `student_clearance` (
                    `id` INT AUTO_INCREMENT PRIMARY KEY,
                    `student_id` INT NOT NULL,
                    `department_name` VARCHAR(100) NOT NULL,
                    `school_year` VARCHAR(50) NOT NULL,
                    `semester` VARCHAR(50) NOT NULL,
                    `status` VARCHAR(50) NOT NULL DEFAULT 'Cleared',
                    `cleared_by` VARCHAR(150) NOT NULL DEFAULT 'Dean / Head',
                    `cleared_at` DATETIME NULL,
                    `remarks` TEXT NULL,
                    UNIQUE KEY `uniq_stud_dept` (`student_id`, `department_name`, `school_year`, `semester`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

            var depts = new[] {
                ("Office of the University Registrar", "Dr. Rosalinda Santos", "Cleared", "201 file records & credentials completed"),
                ("Guidance & Counseling Office", "Ma. Lourdes Ramos, RGC", "Cleared", "No pending behavioral sanctions"),
                ("University Library Department", "Head Librarian", "Cleared", "Zero overdue book accounts or damage fines"),
                ("Finance & Accounting Office", "Bursar Officer", "Cleared", "Full matriculation cleared & settled"),
                ("Dean of Academic Affairs", "College Dean", "Cleared", "Academic curriculum progression in order")
            };

            foreach (var (dept, officer, status, note) in depts)
            {
                await _db.ExecuteAsync(@"
                    INSERT IGNORE INTO `student_clearance` 
                    (`student_id`, `department_name`, `school_year`, `semester`, `status`, `cleared_by`, `cleared_at`, `remarks`) 
                    VALUES (@studentId, @dept, @schoolYear, @semester, @status, @officer, NOW(), @note)",
                    new { studentId, dept, schoolYear, semester, status, officer, note });
            }
        }
        catch
        {
            // non-fatal
        }
    }

    public async Task<List<ClearanceRecord>> GetClearanceRecordsAsync(int studentId, string? schoolYear = null, string? semester = null)
    {
        if (string.IsNullOrEmpty(schoolYear) || string.IsNullOrEmpty(semester))
        {
            var term = await GetActiveTermAsync();
            schoolYear = term.SchoolYear;
            semester = term.Semester;
        }

        await EnsureClearanceRowsAsync(studentId, schoolYear, semester);

        var sql = @"
            SELECT id, student_id AS StudentId, department_name AS DepartmentName, 
                   school_year AS SchoolYear, semester AS Semester, status, 
                   cleared_by AS ClearedBy, cleared_at AS ClearedAt, remarks AS Remarks 
            FROM `student_clearance` 
            WHERE student_id = @studentId AND school_year = @schoolYear AND semester = @semester 
            ORDER BY id ASC";

        var list = await _db.QueryAsync<ClearanceRecord>(sql, new { studentId, schoolYear, semester });
        return list.AsList();
    }

    // ─── Tuition & Finance Payments (Finance Flow) ───────────────────────────

    public async Task EnsurePaymentRowsAsync(int studentId)
    {
        try
        {
            await _db.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS `student_payments` (
                    `id` INT AUTO_INCREMENT PRIMARY KEY,
                    `student_id` INT NOT NULL,
                    `receipt_no` VARCHAR(60) NOT NULL UNIQUE,
                    `amount` DECIMAL(10,2) NOT NULL,
                    `pay_date` DATETIME NOT NULL,
                    `payment_method` VARCHAR(50) NOT NULL,
                    `status` VARCHAR(30) NOT NULL DEFAULT 'Paid',
                    `description` VARCHAR(255) NOT NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

            var count = await _db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM student_payments WHERE student_id = @studentId", new { studentId });

            if (count == 0)
            {
                var rand = new Random(studentId);
                var rcpt = $"NUL-OR-{DateTime.UtcNow.Year}-{rand.Next(100000, 999999)}";
                await _db.ExecuteAsync(@"
                    INSERT INTO student_payments 
                    (student_id, receipt_no, amount, pay_date, payment_method, status, description) 
                    VALUES (@studentId, @rcpt, 28500.00, NOW() - INTERVAL 15 DAY, 'Online Banking (BDO/Maya)', 'Paid', 'AY 2025-2026 1st Sem Tuition & Laboratory Fees')",
                    new { studentId, rcpt });
            }
        }
        catch
        {
            // non-fatal
        }
    }

    public async Task<List<PaymentRecord>> GetPaymentsAsync(int studentId)
    {
        await EnsurePaymentRowsAsync(studentId);

        var sql = @"
            SELECT id, student_id AS StudentId, receipt_no AS ReceiptNo, 
                   amount, pay_date AS PayDate, payment_method AS PaymentMethod, 
                   status, description AS Description 
            FROM student_payments 
            WHERE student_id = @studentId 
            ORDER BY pay_date DESC, id DESC";

        var list = await _db.QueryAsync<PaymentRecord>(sql, new { studentId });
        return list.AsList();
    }

    public async Task<string> SubmitPaymentAsync(int studentId, decimal amount, string method, string desc)
    {
        await EnsurePaymentRowsAsync(studentId);
        var rcpt = $"NUL-OR-{DateTime.UtcNow.Year}-{Random.Shared.Next(100000, 999999)}";

        await _db.ExecuteAsync(@"
            INSERT INTO student_payments 
            (student_id, receipt_no, amount, pay_date, payment_method, status, description) 
            VALUES (@studentId, @rcpt, @amount, NOW(), @method, 'Paid', @desc)",
            new { studentId, rcpt, amount, method, desc });

        await LogActivityAsync(studentId, $"Processed payment of ₱{amount:N2} (Receipt #{rcpt}) via Student Portal");
        return rcpt;
    }

    // ─── Library Books & Reservations (Library Flow) ─────────────────────────

    public async Task EnsureLibraryRowsAsync(int studentId)
    {
        try
        {
            await _db.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS `library_reservations` (
                    `id` INT AUTO_INCREMENT PRIMARY KEY,
                    `student_id` INT NOT NULL,
                    `book_title` VARCHAR(255) NOT NULL,
                    `book_author` VARCHAR(255) NOT NULL,
                    `reservation_date` DATETIME NOT NULL,
                    `due_date` DATETIME NOT NULL,
                    `status` VARCHAR(50) NOT NULL DEFAULT 'Active'
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

            var count = await _db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM library_reservations WHERE student_id = @studentId", new { studentId });

            if (count == 0)
            {
                await _db.ExecuteAsync(@"
                    INSERT INTO library_reservations 
                    (student_id, book_title, book_author, reservation_date, due_date, status) 
                    VALUES (@studentId, 'Database System Concepts (7th Edition)', 'Silberschatz, Korth & Sudarshan', NOW() - INTERVAL 3 DAY, NOW() + INTERVAL 4 DAY, 'Active')",
                    new { studentId });
            }
        }
        catch
        {
            // non-fatal
        }
    }

    public async Task<List<LibraryReservationRecord>> GetLibraryReservationsAsync(int studentId)
    {
        await EnsureLibraryRowsAsync(studentId);

        var sql = @"
            SELECT id, student_id AS StudentId, book_title AS BookTitle, 
                   book_author AS BookAuthor, reservation_date AS ReservationDate, 
                   due_date AS DueDate, status 
            FROM library_reservations 
            WHERE student_id = @studentId 
            ORDER BY reservation_date DESC";

        var list = await _db.QueryAsync<LibraryReservationRecord>(sql, new { studentId });
        return list.AsList();
    }

    public async Task<int> SubmitLibraryReservationAsync(int studentId, string bookTitle, string bookAuthor)
    {
        await EnsureLibraryRowsAsync(studentId);

        var sql = @"
            INSERT INTO library_reservations 
            (student_id, book_title, book_author, reservation_date, due_date, status) 
            VALUES (@studentId, @bookTitle, @bookAuthor, NOW(), NOW() + INTERVAL 7 DAY, 'Active')";

        await _db.ExecuteAsync(sql, new { studentId, bookTitle, bookAuthor });
        var id = await _db.QueryFirstOrDefaultAsync<int>("SELECT LAST_INSERT_ID()");

        await LogActivityAsync(studentId, $"Reserved Library Book '{bookTitle}' via Student Portal");
        return id;
    }

    // ─── Scholarships (Scholarships Flow) ────────────────────────────────────

    public async Task EnsureScholarshipRowsAsync(int studentId)
    {
        try
        {
            await _db.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS `scholarship_applications` (
                    `id` INT AUTO_INCREMENT PRIMARY KEY,
                    `student_id` INT NOT NULL,
                    `scholarship_name` VARCHAR(150) NOT NULL,
                    `school_year` VARCHAR(50) NOT NULL,
                    `semester` VARCHAR(50) NOT NULL,
                    `requirements_submitted` TINYINT(1) NOT NULL DEFAULT 1,
                    `status` VARCHAR(50) NOT NULL DEFAULT 'under_review',
                    `requested_at` DATETIME NOT NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");
        }
        catch
        {
            // non-fatal
        }
    }

    public List<ScholarshipItem> GetAvailableScholarships()
    {
        return new List<ScholarshipItem>
        {
            new() { Id = 1, Name = "NU President's Academic Scholarship", Description = "100% Tuition discount for students with GWA 1.00 - 1.25", SlotsAvailable = 15 },
            new() { Id = 2, Name = "Collegiate Dean's Lister Grant", Description = "50% Tuition discount for students with GWA 1.26 - 1.50", SlotsAvailable = 30 },
            new() { Id = 3, Name = "CHED Tulong Dunong Program (TDP-TES)", Description = "Government subsidy allowance for qualified collegiate students", SlotsAvailable = 50 },
            new() { Id = 4, Name = "NU Athletic & Cultural Varsity Grant", Description = "Full athletic waiver for university varsity team members", SlotsAvailable = 20 }
        };
    }

    public async Task<List<ScholarshipApplicationRecord>> GetScholarshipApplicationsAsync(int studentId)
    {
        await EnsureScholarshipRowsAsync(studentId);

        var sql = @"
            SELECT id, student_id AS StudentId, scholarship_name AS ScholarshipName, 
                   school_year AS SchoolYear, semester AS Semester, 
                   requirements_submitted AS RequirementsSubmitted, status, 
                   requested_at AS RequestedAt 
            FROM scholarship_applications 
            WHERE student_id = @studentId 
            ORDER BY requested_at DESC";

        var list = await _db.QueryAsync<ScholarshipApplicationRecord>(sql, new { studentId });
        return list.AsList();
    }

    public async Task<int> SubmitScholarshipApplicationAsync(int studentId, string scholarName, string sy, string sem)
    {
        await EnsureScholarshipRowsAsync(studentId);

        var sql = @"
            INSERT INTO scholarship_applications 
            (student_id, scholarship_name, school_year, semester, requirements_submitted, status, requested_at) 
            VALUES (@studentId, @scholarName, @sy, @sem, 1, 'under_review', NOW())";

        await _db.ExecuteAsync(sql, new { studentId, scholarName, sy, sem });
        var id = await _db.QueryFirstOrDefaultAsync<int>("SELECT LAST_INSERT_ID()");

        await LogActivityAsync(studentId, $"Applied for '{scholarName}' grant via Student Portal");
        return id;
    }

    // ─── Assignments & Coursework (Faculty LMS Flow) ─────────────────────────

    public async Task EnsureAssignmentRowsAsync(int studentId)
    {
        try
        {
            await _db.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS `student_assignments` (
                    `id` INT AUTO_INCREMENT PRIMARY KEY,
                    `student_id` INT NOT NULL,
                    `subject_code` VARCHAR(50) NOT NULL,
                    `subject_name` VARCHAR(150) NOT NULL,
                    `title` VARCHAR(255) NOT NULL,
                    `description` TEXT NULL,
                    `max_score` DECIMAL(5,2) NOT NULL DEFAULT 100,
                    `due_date` DATETIME NOT NULL,
                    `status` VARCHAR(50) NOT NULL DEFAULT 'Assigned',
                    `is_submitted` TINYINT(1) NOT NULL DEFAULT 0,
                    `my_score` DECIMAL(5,2) NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

            var count = await _db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM student_assignments WHERE student_id = @studentId", new { studentId });

            if (count == 0)
            {
                var sampleAssignments = new[] {
                    ("IT104", "Data Structures & Algorithms", "Laboratory Exercise 4: Binary Search Trees Implementation", "Submit source code .zip and test output documentation.", 100m, DateTime.UtcNow.AddDays(3)),
                    ("IT201", "Database Systems", "Midterm Case Study: E-Commerce Relational Schema Design", "Normalize up to 3NF with ER diagram and MySQL DDL script.", 100m, DateTime.UtcNow.AddDays(5)),
                    ("GE102", "Purposive Communication", "Term Synthesis Essay on Digital Ethics in Higher Education", "1,500 words APA format research commentary.", 50m, DateTime.UtcNow.AddDays(7))
                };

                foreach (var (code, name, title, desc, max, due) in sampleAssignments)
                {
                    await _db.ExecuteAsync(@"
                        INSERT INTO student_assignments 
                        (student_id, subject_code, subject_name, title, description, max_score, due_date, status, is_submitted) 
                        VALUES (@studentId, @code, @name, @title, @desc, @max, @due, 'Assigned', 0)",
                        new { studentId, code, name, title, desc, max, due });
                }
            }
        }
        catch
        {
            // non-fatal
        }
    }

    public async Task<List<AssignmentItem>> GetAssignmentsAsync(int studentId)
    {
        await EnsureAssignmentRowsAsync(studentId);

        var sql = @"
            SELECT id, 0 AS ClassOfferingId, subject_code AS SubjectCode, 
                   subject_name AS SubjectName, title, description, 
                   max_score AS MaxScore, due_date AS DueDate, status, 
                   is_submitted AS IsSubmitted, my_score AS MyScore 
            FROM student_assignments 
            WHERE student_id = @studentId 
            ORDER BY due_date ASC";

        var list = await _db.QueryAsync<AssignmentItem>(sql, new { studentId });
        return list.AsList();
    }

    public async Task SubmitAssignmentWorkAsync(int assignmentId, int studentId)
    {
        await EnsureAssignmentRowsAsync(studentId);

        await _db.ExecuteAsync(@"
            UPDATE student_assignments 
            SET is_submitted = 1, status = 'Submitted &bull; Pending Review' 
            WHERE id = @assignmentId AND student_id = @studentId",
            new { assignmentId, studentId });

        await LogActivityAsync(studentId, $"Submitted coursework assignment (ID #{assignmentId}) via Student Portal");
    }
}

