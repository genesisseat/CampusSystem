using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CampusSystem.Data.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RegistrarMain.Services;

namespace RegistrarMain.Pages;

public class DocumentProcessingModel : PageModel
{
    private readonly DatabaseService _db;
    private readonly CampusJsonDb _jsonDb;

    public DocumentProcessingModel(DatabaseService db, CampusJsonDb jsonDb)
    {
        _db = db;
        _jsonDb = jsonDb;
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

    public Task<IActionResult> OnGetAsync(string tab = "queue", string status_filter = "", int req_id = 0)
    {
        _db.EnsureRegistrarSession();
        ActiveTab = new[] { "queue", "issue", "print_doc" }.Contains(tab) ? tab : "queue";
        StatusFilter = status_filter?.Trim() ?? "";
        PrintReqId = req_id;

        var users = _jsonDb.GetUsers();
        var profiles = _jsonDb.GetStudentProfiles();
        var requests = _jsonDb.GetTranscriptRequests();

        StudentsList = users
            .Where(u => u.Role == "student")
            .Select(u =>
            {
                var p = profiles.FirstOrDefault(x => x.StudentId == u.Id);
                return new StudentSimpleItem
                {
                    Id = u.Id,
                    StudentIdNumber = u.StudentIdNumber,
                    Name = u.Name,
                    Program = p?.Program ?? "BS Information Technology"
                };
            })
            .OrderBy(s => s.Name)
            .ToList();

        var qList = requests.AsEnumerable();
        if (!string.IsNullOrEmpty(StatusFilter))
        {
            qList = qList.Where(r => r.Status.Equals(StatusFilter, StringComparison.OrdinalIgnoreCase));
        }

        RequestsQueue = qList
            .Select(tr =>
            {
                var u = users.FirstOrDefault(x => x.Id == tr.StudentId);
                var p = profiles.FirstOrDefault(x => x.StudentId == tr.StudentId);

                return new DocumentQueueItem
                {
                    Id = tr.Id,
                    StudentId = tr.StudentId,
                    DocumentType = tr.DocumentType,
                    Purpose = tr.Purpose,
                    Copies = tr.Copies,
                    Status = tr.Status,
                    QrCodeToken = tr.QrCodeToken,
                    Remarks = tr.Remarks,
                    RequestedAt = tr.RequestedAt,
                    StudentName = u?.Name ?? tr.StudentName,
                    StudentIdNumber = u?.StudentIdNumber ?? tr.StudentNumber,
                    Email = u?.Email ?? $"{tr.StudentId}@student.campus.edu",
                    Program = p?.Program ?? "BS Information Technology",
                    YearLevel = p != null ? $"{p.YearLevel}nd Year" : "2nd Year"
                };
            })
            .OrderByDescending(r => r.RequestedAt)
            .ToList();

        if (PrintReqId > 0)
        {
            var tr = requests.FirstOrDefault(r => r.Id == PrintReqId);
            if (tr != null)
            {
                var u = users.FirstOrDefault(x => x.Id == tr.StudentId);
                var p = profiles.FirstOrDefault(x => x.StudentId == tr.StudentId);

                PrintData = new PrintDataDetail
                {
                    Id = tr.Id,
                    StudentId = tr.StudentId,
                    StudentName = u?.Name ?? tr.StudentName,
                    StudentIdNumber = u?.StudentIdNumber ?? tr.StudentNumber,
                    Email = u?.Email ?? $"{tr.StudentId}@student.campus.edu",
                    Program = p?.Program ?? "BS Information Technology",
                    YearLevel = p != null ? $"{p.YearLevel}nd Year" : "2nd Year",
                    AverageGrade = p?.Gwa ?? 1.50m,
                    DocumentType = tr.DocumentType,
                    Purpose = tr.Purpose,
                    QrCodeToken = tr.QrCodeToken,
                    RequestedAt = tr.RequestedAt
                };

                var enrolledSubs = _jsonDb.GetEnrolledSubjects().Where(es => es.StudentId == tr.StudentId).ToList();
                var grades = _jsonDb.GetGrades().Where(g => g.StudentId == tr.StudentId).ToList();

                PrintStudentGrades = enrolledSubs.Select(es =>
                {
                    var g = grades.FirstOrDefault(x => x.SubjectCode == es.SubjectCode);
                    decimal? numGrade = decimal.TryParse(g?.FinalGrade, out var val) ? val : 1.50m;
                    return new PrintStudentGradeItem
                    {
                        SubjectCode = es.SubjectCode,
                        SubjectName = es.SubjectTitle,
                        Units = es.Units,
                        SchoolYear = "2026-2027",
                        Semester = "1st Semester",
                        Grade = numGrade,
                        IsInc = g?.Remarks == "Incomplete",
                        Remarks = g?.Remarks ?? "Passed"
                    };
                }).ToList();
            }
        }

        return Task.FromResult<IActionResult>(Page());
    }

    public Task<IActionResult> OnPostCreateRequestAsync(int student_id, string document_type, string purpose, int copies, string? remarks)
    {
        _db.EnsureRegistrarSession();
        var docType = (document_type ?? "").Trim();
        var p = (purpose ?? "").Trim();

        if (student_id > 0 && !string.IsNullOrEmpty(docType))
        {
            var req = _jsonDb.SubmitTranscriptRequest(student_id, docType, p, Math.Max(1, copies));
            if (!string.IsNullOrWhiteSpace(remarks))
            {
                _jsonDb.UpdateTranscriptRequestStatus(req.Id, "pending", remarks);
            }

            TempData["FlashMessage"] = $"Document request successfully logged with Tracking Token: {req.QrCodeToken}";
            TempData["FlashType"] = "success";
        }
        else
        {
            TempData["FlashMessage"] = "Please select student and document type.";
            TempData["FlashType"] = "error";
        }

        return Task.FromResult<IActionResult>(RedirectToPage("/DocumentProcessing", new { tab = "queue" }));
    }

    public Task<IActionResult> OnPostUpdateStatusAsync(int request_id, string status, string? remarks)
    {
        _db.EnsureRegistrarSession();
        var validStatuses = new[] { "pending", "processing", "ready", "released", "rejected" };
        var newStatus = (status ?? "").Trim().ToLower();

        if (request_id > 0 && validStatuses.Contains(newStatus))
        {
            _jsonDb.UpdateTranscriptRequestStatus(request_id, newStatus, remarks ?? "");

            TempData["FlashMessage"] = $"Request #{request_id} status updated to {char.ToUpper(newStatus[0]) + newStatus.Substring(1)}.";
            TempData["FlashType"] = "success";
        }

        return Task.FromResult<IActionResult>(RedirectToPage("/DocumentProcessing", new { tab = "queue" }));
    }
}
