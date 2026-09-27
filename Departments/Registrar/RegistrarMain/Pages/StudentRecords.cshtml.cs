using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RegistrarMain.Models;
using RegistrarMain.Services;

namespace RegistrarMain.Pages;

public class StudentRecordsModel : PageModel
{
    private readonly DatabaseService _db;

    public StudentRecordsModel(DatabaseService db)
    {
        _db = db;
    }

    public static readonly string[] VaultDocumentTypes = [
        "Form 137",
        "Form 138",
        "Birth Certificate",
        "Good Moral",
        "Transcript from Previous School",
        "Medical Clearance"
    ];

    public static readonly string[] EnrollmentStatuses = ["Active", "On-Leave", "Dropped", "Dismissed", "Graduated"];
    public static readonly string[] AcademicStatuses = ["Good Standing", "Probation", "Graduating", "Graduated", "Disqualified"];

    public int ViewId { get; set; }
    public string Query { get; set; } = "";
    public string ProgramFilter { get; set; } = "";
    public string StatusFilter { get; set; } = "";

    // List view
    public List<StudentListItem> Students { get; set; } = new();
    public List<string> ProgramsList { get; set; } = new();

    // Detail view
    public AppUser? Student { get; set; }
    public StudentProfileDetail Profile { get; set; } = new();
    public List<DocumentCredential> Vault { get; set; } = new();
    public List<StudentGradeRecord> StudentGrades { get; set; } = new();
    public List<TransfereeCreditedRecord> CreditedSubjects { get; set; } = new();

    public class StudentListItem
    {
        public int Id { get; set; }
        public string StudentIdNumber { get; set; } = "";
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Program { get; set; }
        public string? YearLevel { get; set; }
        public string? EnrollmentStatus { get; set; }
        public string? AcademicStatus { get; set; }
        public decimal? AverageGrade { get; set; }
        public int VerifiedCount { get; set; }
        public int TotalDocs { get; set; }
    }

    public class StudentProfileDetail
    {
        public string Program { get; set; } = "Not set";
        public string YearLevel { get; set; } = "1st Year";
        public decimal? AverageGrade { get; set; }
        public string AcademicStatus { get; set; } = "Good Standing";
        public string EnrollmentStatus { get; set; } = "Active";
        public int CompletedUnits { get; set; }
        public int UnitsRemaining { get; set; } = 144;
        public string GuidanceClearanceStatus { get; set; } = "Cleared";
        public string? ContactNumber { get; set; }
        public string? Address { get; set; }
        public string Gender { get; set; } = "Female";
    }

    public class StudentGradeRecord
    {
        public string SubjectCode { get; set; } = "";
        public string SubjectName { get; set; } = "";
        public int Units { get; set; }
        public string SectionCode { get; set; } = "";
        public string SchoolYear { get; set; } = "";
        public string Semester { get; set; } = "";
        public decimal? Grade { get; set; }
        public bool IsInc { get; set; }
        public string? GradeStatus { get; set; }
        public string? Remarks { get; set; }
    }

    public class TransfereeCreditedRecord
    {
        public string PrevSchool { get; set; } = "";
        public string PrevSubjectCode { get; set; } = "";
        public string PrevSubjectTitle { get; set; } = "";
        public int PrevUnits { get; set; }
        public decimal PrevGrade { get; set; }
        public string InstitutionalCode { get; set; } = "";
        public string InstitutionalName { get; set; } = "";
        public string Status { get; set; } = "Approved";
    }

    private async Task EnsureVaultRowsAsync(int studentId)
    {
        var conn = _db.Connection;
        foreach (var type in VaultDocumentTypes)
        {
            await conn.ExecuteAsync(
                "INSERT IGNORE INTO document_credentials (student_id, document_type) VALUES (@studentId, @type)",
                new { studentId, type });
        }
    }

    public async Task<IActionResult> OnGetAsync(int id = 0, string q = "", string program = "", string status = "")
    {
        _db.EnsureRegistrarSession();
        ViewId = id;
        Query = q?.Trim() ?? "";
        ProgramFilter = program?.Trim() ?? "";
        StatusFilter = status?.Trim() ?? "";

        var conn = _db.Connection;

        if (ViewId > 0)
        {
            Student = await conn.QueryFirstOrDefaultAsync<AppUser>(
                "SELECT id, student_id_number AS StudentIdNumber, name, email, created_at AS CreatedAt FROM `user` WHERE id = @id AND role = 'student'",
                new { id = ViewId });

            if (Student == null)
            {
                return RedirectToPage("/StudentRecords");
            }

            await EnsureVaultRowsAsync(ViewId);

            var p = await conn.QueryFirstOrDefaultAsync<StudentProfileDetail>(
                "SELECT program, year_level AS YearLevel, average_grade AS AverageGrade, academic_status AS AcademicStatus, " +
                "enrollment_status AS EnrollmentStatus, completed_units AS CompletedUnits, guidance_clearance_status AS GuidanceClearanceStatus, " +
                "contact_number AS ContactNumber, address AS Address, gender AS Gender FROM student_profile WHERE user_id = @id",
                new { id = ViewId });

            if (p != null) Profile = p;

            var docs = await conn.QueryAsync<DocumentCredential>(
                "SELECT id, document_type AS DocumentType, status, remarks, file_path AS FilePath, submitted_at AS SubmittedAt, verified_at AS VerifiedAt " +
                "FROM document_credentials WHERE student_id = @id ORDER BY id ASC",
                new { id = ViewId });
            Vault = docs.AsList();

            var grades = await conn.QueryAsync<StudentGradeRecord>(
                "SELECT s.subject_code AS SubjectCode, s.subject_name AS SubjectName, s.units AS Units, co.section_code AS SectionCode, " +
                "e.school_year AS SchoolYear, e.semester AS Semester, g.grade AS Grade, g.is_inc AS IsInc, g.status AS GradeStatus, g.remarks AS Remarks " +
                "FROM enrolled_subjects es " +
                "JOIN enrollments e ON e.id = es.enrollment_id " +
                "JOIN class_offerings co ON co.id = es.class_offering_id " +
                "JOIN subjects s ON s.id = co.subject_id " +
                "LEFT JOIN grades g ON g.enrolled_subject_id = es.id " +
                "WHERE e.student_id = @id " +
                "ORDER BY e.school_year DESC, e.semester DESC",
                new { id = ViewId });
            StudentGrades = grades.AsList();

            var credited = await conn.QueryAsync<TransfereeCreditedRecord>(
                "SELECT tcs.prev_school AS PrevSchool, tcs.prev_subject_code AS PrevSubjectCode, tcs.prev_subject_title AS PrevSubjectTitle, " +
                "tcs.prev_units AS PrevUnits, tcs.prev_grade AS PrevGrade, s.subject_code AS InstitutionalCode, s.subject_name AS InstitutionalName, tcs.status " +
                "FROM transferee_credited_subjects tcs " +
                "JOIN subjects s ON s.id = tcs.credited_to_subject_id " +
                "WHERE tcs.student_id = @id",
                new { id = ViewId });
            CreditedSubjects = credited.AsList();
        }
        else
        {
            var sql = "SELECT u.id, u.student_id_number AS StudentIdNumber, u.name, u.email, sp.program, sp.year_level AS YearLevel, " +
                      "sp.enrollment_status AS EnrollmentStatus, sp.academic_status AS AcademicStatus, sp.average_grade AS AverageGrade, " +
                      "(SELECT COUNT(*) FROM document_credentials dc WHERE dc.student_id = u.id AND dc.status = 'Verified') AS VerifiedCount, " +
                      "(SELECT COUNT(*) FROM document_credentials dc WHERE dc.student_id = u.id) AS TotalDocs " +
                      "FROM `user` u " +
                      "LEFT JOIN student_profile sp ON sp.user_id = u.id " +
                      "WHERE u.role = 'student'";

            var prms = new DynamicParameters();
            if (!string.IsNullOrEmpty(Query))
            {
                sql += " AND (u.name LIKE @q OR u.student_id_number LIKE @q OR u.email LIKE @q)";
                prms.Add("q", $"%{Query}%");
            }
            if (!string.IsNullOrEmpty(ProgramFilter))
            {
                sql += " AND sp.program = @prog";
                prms.Add("prog", ProgramFilter);
            }
            if (!string.IsNullOrEmpty(StatusFilter))
            {
                sql += " AND sp.enrollment_status = @status";
                prms.Add("status", StatusFilter);
            }

            sql += " ORDER BY u.name ASC";

            var rows = await conn.QueryAsync<StudentListItem>(sql, prms);
            Students = rows.AsList();

            var progs = await conn.QueryAsync<string>(
                "SELECT DISTINCT program FROM student_profile WHERE program IS NOT NULL AND program != '' ORDER BY program");
            ProgramsList = progs.AsList();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCreateStudentAsync(
        string student_id_number, string name, string email, string program, string year_level, string gender, string? contact_number)
    {
        _db.EnsureRegistrarSession();
        var idNumber = (student_id_number ?? "").Trim();
        var studentName = (name ?? "").Trim();
        var studentEmail = (email ?? "").Trim();

        if (!string.IsNullOrEmpty(idNumber) && !string.IsNullOrEmpty(studentName) && !string.IsNullOrEmpty(studentEmail))
        {
            var conn = _db.Connection;
            await conn.OpenAsync();
            using var trans = await conn.BeginTransactionAsync();
            try
            {
                var pwd = BCrypt.Net.BCrypt.HashPassword("password123");
                var sqlUser = "INSERT INTO `user` (student_id_number, name, email, password, role) VALUES (@idNumber, @studentName, @studentEmail, @pwd, 'student'); SELECT LAST_INSERT_ID();";
                var newId = await conn.ExecuteScalarAsync<int>(sqlUser, new { idNumber, studentName, studentEmail, pwd }, trans);

                var sqlProfile = "INSERT INTO student_profile (user_id, program, year_level, gender, contact_number, academic_status, enrollment_status) " +
                                 "VALUES (@newId, @program, @year_level, @gender, @contact_number, 'Good Standing', 'Active');";
                await conn.ExecuteAsync(sqlProfile, new { newId, program, year_level, gender, contact_number }, trans);

                foreach (var type in VaultDocumentTypes)
                {
                    await conn.ExecuteAsync(
                        "INSERT IGNORE INTO document_credentials (student_id, document_type) VALUES (@newId, @type)",
                        new { newId, type }, trans);
                }

                await trans.CommitAsync();

                await _db.LogActivityAsync(newId, $"New student record created: {studentName} ({idNumber}) by {_db.CurrentUserName}");
                TempData["FlashMessage"] = $"Student {studentName} successfully created with ID {idNumber}.";
                TempData["FlashType"] = "success";
                return RedirectToPage("/StudentRecords", new { id = newId });
            }
            catch (Exception ex)
            {
                await trans.RollbackAsync();
                TempData["FlashMessage"] = "Error creating student: " + ex.Message;
                TempData["FlashType"] = "error";
            }
        }
        else
        {
            TempData["FlashMessage"] = "Please fill in all required student details.";
            TempData["FlashType"] = "error";
        }

        return RedirectToPage("/StudentRecords");
    }

    public async Task<IActionResult> OnPostUpdateStudentStatusAsync(int student_id, string enrollment_status, string academic_status, string guidance_clearance_status)
    {
        _db.EnsureRegistrarSession();
        if (student_id > 0 && EnrollmentStatuses.Contains(enrollment_status) && AcademicStatuses.Contains(academic_status))
        {
            var conn = _db.Connection;
            await conn.ExecuteAsync(
                "UPDATE student_profile SET enrollment_status = @enr, academic_status = @acad, guidance_clearance_status = @guid WHERE user_id = @sid",
                new { enr = enrollment_status, acad = academic_status, guid = guidance_clearance_status, sid = student_id });

            await _db.LogActivityAsync(student_id, $"Student status updated: Enr={enrollment_status}, Acad={academic_status}, Guidance={guidance_clearance_status} by {_db.CurrentUserName}");
            TempData["FlashMessage"] = "Student status updated.";
            TempData["FlashType"] = "success";
        }
        return RedirectToPage("/StudentRecords", new { id = student_id });
    }

    public async Task<IActionResult> OnPostUpdateDocumentAsync(int student_id, int doc_id, string doc_status, string? remarks)
    {
        _db.EnsureRegistrarSession();
        if (student_id > 0 && doc_id > 0 && new[] { "Missing", "Submitted", "Verified" }.Contains(doc_status))
        {
            var conn = _db.Connection;
            var now = DateTime.Now;
            var r = (remarks ?? "").Trim();

            if (doc_status == "Missing")
            {
                await conn.ExecuteAsync(
                    "UPDATE document_credentials SET status = @st, remarks = @r, submitted_at = NULL, verified_at = NULL WHERE id = @id AND student_id = @sid",
                    new { st = doc_status, r, id = doc_id, sid = student_id });
            }
            else if (doc_status == "Submitted")
            {
                await conn.ExecuteAsync(
                    "UPDATE document_credentials SET status = @st, remarks = @r, submitted_at = COALESCE(submitted_at, @now), verified_at = NULL WHERE id = @id AND student_id = @sid",
                    new { st = doc_status, r, now, id = doc_id, sid = student_id });
            }
            else // Verified
            {
                await conn.ExecuteAsync(
                    "UPDATE document_credentials SET status = @st, remarks = @r, submitted_at = COALESCE(submitted_at, @now), verified_at = @now WHERE id = @id AND student_id = @sid",
                    new { st = doc_status, r, now, id = doc_id, sid = student_id });
            }

            await _db.LogActivityAsync(student_id, $"Document credential status updated to \"{doc_status}\" by {_db.CurrentUserName}");
            TempData["FlashMessage"] = "Document record updated.";
            TempData["FlashType"] = "success";
        }

        return RedirectToPage("/StudentRecords", new { id = student_id });
    }
}
