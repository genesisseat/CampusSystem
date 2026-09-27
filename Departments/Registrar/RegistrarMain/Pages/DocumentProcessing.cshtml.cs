using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RegistrarMain.Models;
using RegistrarMain.Services;

namespace RegistrarMain.Pages;

public class DocumentProcessingModel : PageModel
{
    private readonly DatabaseService _db;

    public DocumentProcessingModel(DatabaseService db)
    {
        _db = db;
    }

    public string ActiveTab { get; set; } = "queue";
    public string StatusFilter { get; set; } = "";
    public List<DocumentQueueItem> RequestsQueue { get; set; } = new();
    public List<StudentSimpleItem> StudentsList { get; set; } = new();

    public int PrintReqId { get; set; }
    public PrintDataDetail? PrintData { get; set; }
    public List<PrintStudentGradeItem> PrintStudentGrades { get; set; } = new();

    public class DocumentQueueItem
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = "";
        public string StudentIdNumber { get; set; } = "";
        public string Email { get; set; } = "";
        public string Program { get; set; } = "";
        public string YearLevel { get; set; } = "";
        public string DocumentType { get; set; } = "";
        public string Purpose { get; set; } = "";
        public int Copies { get; set; }
        public string Status { get; set; } = "pending";
        public string QrCodeToken { get; set; } = "";
        public string? Remarks { get; set; }
        public DateTime RequestedAt { get; set; }
    }

    public class StudentSimpleItem
    {
        public int Id { get; set; }
        public string StudentIdNumber { get; set; } = "";
        public string Name { get; set; } = "";
        public string Program { get; set; } = "";
    }

    public class PrintDataDetail
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = "";
        public string StudentIdNumber { get; set; } = "";
        public string Email { get; set; } = "";
        public string Program { get; set; } = "";
        public string YearLevel { get; set; } = "";
        public decimal? AverageGrade { get; set; }
        public string DocumentType { get; set; } = "";
        public string Purpose { get; set; } = "";
        public string QrCodeToken { get; set; } = "";
        public DateTime RequestedAt { get; set; }
    }

    public class PrintStudentGradeItem
    {
        public string SubjectCode { get; set; } = "";
        public string SubjectName { get; set; } = "";
        public decimal Units { get; set; }
        public string SchoolYear { get; set; } = "";
        public string Semester { get; set; } = "";
        public decimal? Grade { get; set; }
        public bool IsInc { get; set; }
        public string? Remarks { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(string tab = "queue", string status_filter = "", int req_id = 0)
    {
        _db.EnsureRegistrarSession();
        ActiveTab = new[] { "queue", "issue", "print_doc" }.Contains(tab) ? tab : "queue";
        StatusFilter = status_filter?.Trim() ?? "";
        PrintReqId = req_id;

        var conn = _db.Connection;

        var students = await conn.QueryAsync<StudentSimpleItem>(
            "SELECT u.id, u.student_id_number AS StudentIdNumber, u.name, sp.program " +
            "FROM `user` u JOIN student_profile sp ON sp.user_id = u.id WHERE u.role = 'student' ORDER BY u.name ASC");
        StudentsList = students.AsList();

        var sql = "SELECT tr.id, tr.student_id AS StudentId, tr.document_type AS DocumentType, tr.purpose, tr.copies, tr.status, " +
                  "tr.qr_code_token AS QrCodeToken, tr.remarks, tr.requested_at AS RequestedAt, " +
                  "u.name AS StudentName, u.student_id_number AS StudentIdNumber, u.email, sp.program, sp.year_level AS YearLevel " +
                  "FROM transcript_requests tr " +
                  "JOIN `user` u ON u.id = tr.student_id " +
                  "LEFT JOIN student_profile sp ON sp.user_id = u.id";

        var prms = new DynamicParameters();
        if (!string.IsNullOrEmpty(StatusFilter))
        {
            sql += " WHERE tr.status = @st";
            prms.Add("st", StatusFilter);
        }
        sql += " ORDER BY tr.requested_at DESC";

        var qList = await conn.QueryAsync<DocumentQueueItem>(sql, prms);
        RequestsQueue = qList.AsList();

        if (PrintReqId > 0)
        {
            PrintData = await conn.QueryFirstOrDefaultAsync<PrintDataDetail>(
                "SELECT tr.id, tr.student_id AS StudentId, tr.document_type AS DocumentType, tr.purpose, tr.qr_code_token AS QrCodeToken, tr.requested_at AS RequestedAt, " +
                "u.name AS StudentName, u.student_id_number AS StudentIdNumber, u.email, sp.program, sp.year_level AS YearLevel, sp.average_grade AS AverageGrade " +
                "FROM transcript_requests tr " +
                "JOIN `user` u ON u.id = tr.student_id " +
                "LEFT JOIN student_profile sp ON sp.user_id = u.id " +
                "WHERE tr.id = @id",
                new { id = PrintReqId });

            if (PrintData != null)
            {
                var grades = await conn.QueryAsync<PrintStudentGradeItem>(
                    "SELECT s.subject_code AS SubjectCode, s.subject_name AS SubjectName, s.units, e.school_year AS SchoolYear, e.semester, " +
                    "g.grade, g.is_inc AS IsInc, g.remarks " +
                    "FROM enrolled_subjects es " +
                    "JOIN enrollments e ON e.id = es.enrollment_id " +
                    "JOIN class_offerings co ON co.id = es.class_offering_id " +
                    "JOIN subjects s ON s.id = co.subject_id " +
                    "LEFT JOIN grades g ON g.enrolled_subject_id = es.id " +
                    "WHERE e.student_id = @sid " +
                    "ORDER BY e.school_year ASC, e.semester ASC, s.subject_code ASC",
                    new { sid = PrintData.StudentId });
                PrintStudentGrades = grades.AsList();
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCreateRequestAsync(int student_id, string document_type, string purpose, int copies, string? remarks)
    {
        _db.EnsureRegistrarSession();
        var docType = (document_type ?? "").Trim();
        var p = (purpose ?? "").Trim();

        if (student_id > 0 && !string.IsNullOrEmpty(docType))
        {
            var letters = Regex.Replace(docType, "[^A-Za-z]", "").ToUpper();
            var prefix = letters.Length >= 3 ? letters.Substring(0, 3) : "DOC";
            var randomHex = Convert.ToHexString(RandomNumberGenerator.GetBytes(3));
            var qrToken = $"NU-{prefix}-{DateTime.Now.Year}-{randomHex}";

            var conn = _db.Connection;
            await conn.ExecuteAsync(
                "INSERT INTO transcript_requests (student_id, document_type, purpose, copies, status, qr_code_token, remarks, requested_at) " +
                "VALUES (@student_id, @docType, @p, @copies, 'pending', @qrToken, @remarks, NOW())",
                new { student_id, docType, p, copies = Math.Max(1, copies), qrToken, remarks = (remarks ?? "").Trim() });

            await _db.LogActivityAsync(student_id, $"Document request for {docType} created (Token: {qrToken}) by {_db.CurrentUserName}.");
            TempData["FlashMessage"] = $"Document request successfully logged with Tracking Token: {qrToken}";
            TempData["FlashType"] = "success";
        }
        else
        {
            TempData["FlashMessage"] = "Please select student and document type.";
            TempData["FlashType"] = "error";
        }

        return RedirectToPage("/DocumentProcessing", new { tab = "queue" });
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync(int request_id, string status, string? remarks)
    {
        _db.EnsureRegistrarSession();
        var validStatuses = new[] { "pending", "processing", "ready", "released", "rejected" };
        var newStatus = (status ?? "").Trim().ToLower();

        if (request_id > 0 && validStatuses.Contains(newStatus))
        {
            var conn = _db.Connection;
            var sql = "UPDATE transcript_requests SET status = @newStatus, remarks = @remarks";
            if (newStatus == "processing") sql += ", processed_at = NOW()";
            else if (newStatus == "released") sql += ", released_at = NOW()";
            sql += " WHERE id = @id";

            await conn.ExecuteAsync(sql, new { newStatus, remarks = (remarks ?? "").Trim(), id = request_id });

            var sRow = await conn.QueryFirstOrDefaultAsync<(int StudentId, string DocumentType)>(
                "SELECT student_id AS StudentId, document_type AS DocumentType FROM transcript_requests WHERE id = @id",
                new { id = request_id });

            if (sRow.StudentId > 0)
            {
                await _db.LogActivityAsync(sRow.StudentId, $"Document request #{request_id} ({sRow.DocumentType}) marked as {newStatus} by {_db.CurrentUserName}.");
            }

            TempData["FlashMessage"] = $"Request #{request_id} status updated to {char.ToUpper(newStatus[0]) + newStatus.Substring(1)}.";
            TempData["FlashType"] = "success";
        }

        return RedirectToPage("/DocumentProcessing", new { tab = "queue" });
    }
}
