using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CampusSystem.Data.Models;
using CampusSystem.Data.Services;
using Microsoft.AspNetCore.Http;
using MySqlConnector;
using StudentPortalMain.Models;

namespace StudentPortalMain.Services;

public class StudentPortalDbService
{
    private readonly CampusJsonDb _jsonDb;
    private readonly IHttpContextAccessor _ctx;

    public StudentPortalDbService(CampusJsonDb jsonDb, IHttpContextAccessor ctx)
    {
        _jsonDb = jsonDb;
        _ctx = ctx;
    }

    public MySqlConnection Connection => new MySqlConnection("Server=localhost;Database=dummy");

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
        var first = _jsonDb.GetUsers().FirstOrDefault(u => u.Role == "student" && u.Status == "active");
        return first?.Id ?? 5; // Alyssa Bea Mendoza
    }

    public void SwitchStudent(int studentId)
    {
        CurrentStudentId = studentId;
    }

    public Task<List<StudentUser>> GetAllStudentsAsync()
    {
        var users = _jsonDb.GetUsers()
            .Where(u => u.Role == "student")
            .Select(u => new StudentUser
            {
                Id = u.Id,
                StudentIdNumber = u.StudentIdNumber,
                Name = u.Name,
                Email = u.Email,
                Role = u.Role,
                Status = u.Status,
                CreatedAt = DateTime.UtcNow.AddMonths(-6)
            })
            .ToList();
        return Task.FromResult(users);
    }

    public Task<StudentUser?> GetStudentUserAsync(int studentId)
    {
        var u = _jsonDb.GetUsers().FirstOrDefault(x => x.Id == studentId);
        if (u == null) return Task.FromResult<StudentUser?>(null);

        return Task.FromResult<StudentUser?>(new StudentUser
        {
            Id = u.Id,
            StudentIdNumber = u.StudentIdNumber,
            Name = u.Name,
            Email = u.Email,
            Role = u.Role,
            Status = u.Status,
            CreatedAt = DateTime.UtcNow.AddMonths(-6)
        });
    }

    public Task<(string SchoolYear, string Semester)> GetActiveTermAsync()
    {
        return Task.FromResult(("2026-2027", "1st Semester"));
    }

    public Task LogActivityAsync(int? userId, string message)
    {
        return Task.CompletedTask;
    }

    // ─── Student Profile ─────────────────────────────────────────────────────

    public Task<StudentProfileData> GetStudentProfileAsync(int studentId)
    {
        var profile = _jsonDb.GetStudentProfiles().FirstOrDefault(p => p.StudentId == studentId);
        var enr = _jsonDb.GetEnrollments().FirstOrDefault(e => e.StudentId == studentId);

        var data = new StudentProfileData
        {
            UserId = studentId,
            Program = profile?.Program ?? "BS Information Technology",
            YearLevel = profile != null ? $"{profile.YearLevel}nd Year" : "2nd Year",
            CurriculumYear = "2024-2028",
            AverageGrade = profile?.Gwa ?? 1.50m,
            AcademicStatus = profile?.AcademicStatus ?? "Regular",
            EnrollmentStatus = enr?.Status == "enrolled" ? "Enrolled" : (enr?.Status == "active" ? "Validated" : "Pending"),
            CompletedUnits = 42,
            UnitsRemaining = 102,
            GuidanceClearanceStatus = "Cleared",
            GuidanceNotes = "No disciplinary records on file.",
            ContactNumber = profile?.ContactNumber ?? "0917-555-0101",
            Address = profile?.Address ?? "Manila, Philippines",
            BirthDate = new DateTime(2004, 5, 12),
            Gender = "Female"
        };
        return Task.FromResult(data);
    }

    public Task UpdateContactInfoAsync(int studentId, string contactNumber, string address)
    {
        var profiles = _jsonDb.GetStudentProfiles();
        var prof = profiles.FirstOrDefault(p => p.StudentId == studentId);
        if (prof != null)
        {
            prof.ContactNumber = contactNumber;
            prof.Address = address;
            _jsonDb.SaveStudentProfiles(profiles);
        }
        return Task.CompletedTask;
    }

    // ─── Schedule & Classes ──────────────────────────────────────────────────

    public Task<List<EnrolledClassItem>> GetEnrolledClassesAsync(int studentId, string? schoolYear = null, string? semester = null)
    {
        var sy = schoolYear ?? "2026-2027";
        var sem = semester ?? "1st Semester";

        var enrolledSubs = _jsonDb.GetEnrolledSubjects()
            .Where(es => es.StudentId == studentId && es.Status == "enrolled")
            .ToList();

        var offerings = _jsonDb.GetClassOfferings();
        var list = new List<EnrolledClassItem>();

        foreach (var sub in enrolledSubs)
        {
            var off = offerings.FirstOrDefault(o => o.Id == sub.ClassOfferingId);
            list.Add(new EnrolledClassItem
            {
                EnrolledSubjectId = sub.Id,
                ClassOfferingId = sub.ClassOfferingId,
                SubjectCode = sub.SubjectCode,
                SubjectName = sub.SubjectTitle,
                Units = sub.Units,
                LecHours = 3,
                LabHours = 0,
                SectionCode = off?.SectionCode ?? "BSIT-2A",
                InstructorName = off?.InstructorName ?? "Faculty Member",
                Room = off?.Room ?? "LEC-301",
                DaysOfWeek = off?.DaysOfWeek ?? "MWF",
                StartTime = TimeSpan.FromHours(8),
                EndTime = TimeSpan.FromHours(9.5),
                SchoolYear = sy,
                Semester = sem,
                Status = sub.Status
            });
        }

        return Task.FromResult(list);
    }

    public Task<List<EnrolledClassItem>> GetAllEnrolledClassesAsync(int studentId)
    {
        return GetEnrolledClassesAsync(studentId);
    }

    public Task<List<ClassOfferingRecord>> GetAllClassOfferingsAsync()
    {
        return Task.FromResult(_jsonDb.GetClassOfferings());
    }

    public Task<List<EnrolledClassItem>> GetAvailableOfferingsAsync(string? schoolYear = null, string? semester = null)
    {
        var offerings = _jsonDb.GetClassOfferings();
        var list = offerings.Select(o => new EnrolledClassItem
        {
            ClassOfferingId = o.Id,
            SubjectCode = o.SubjectCode,
            SubjectName = o.SubjectTitle,
            Units = o.Units,
            LecHours = 3,
            LabHours = 0,
            SectionCode = o.SectionCode,
            InstructorName = o.InstructorName,
            Room = o.Room,
            DaysOfWeek = o.DaysOfWeek,
            StartTime = TimeSpan.FromHours(8),
            EndTime = TimeSpan.FromHours(9.5),
            SchoolYear = schoolYear ?? "2026-2027",
            Semester = semester ?? "1st Semester"
        }).ToList();
        return Task.FromResult(list);
    }

    // ─── Grades ──────────────────────────────────────────────────────────────

    public Task<List<TermGradeGroup>> GetGradesGroupedByTermAsync(int studentId)
    {
        return GetStudentGradesGroupedAsync(studentId);
    }

    public Task<decimal?> CalculateCumulativeGwaAsync(int studentId)
    {
        var profile = _jsonDb.GetStudentProfiles().FirstOrDefault(p => p.StudentId == studentId);
        return Task.FromResult<decimal?>(profile?.Gwa ?? 1.50m);
    }

    public Task<List<TermGradeGroup>> GetStudentGradesGroupedAsync(int studentId)
    {
        var grades = _jsonDb.GetGrades().Where(g => g.StudentId == studentId).ToList();
        var list = new List<StudentGradeItem>();

        foreach (var g in grades)
        {
            decimal? numGrade = decimal.TryParse(g.FinalGrade, out var val) ? val : null;
            bool isInc = g.FinalGrade.Equals("INC", StringComparison.OrdinalIgnoreCase);

            list.Add(new StudentGradeItem
            {
                GradeId = g.Id,
                EnrolledSubjectId = g.ClassOfferingId,
                SubjectCode = g.SubjectCode,
                SubjectName = g.SubjectTitle,
                Units = 3,
                SectionCode = "BSIT-2A",
                SchoolYear = "2026-2027",
                Semester = "1st Semester",
                Grade = numGrade,
                IsInc = isInc,
                GradeStatus = "official",
                Remarks = g.Remarks
            });
        }

        var group = new TermGradeGroup
        {
            SchoolYear = "2026-2027",
            Semester = "1st Semester",
            Grades = list,
            SemesterGpa = list.Any(g => g.Grade.HasValue) ? Math.Round(list.Where(g => g.Grade.HasValue).Average(g => g.Grade!.Value), 2) : 1.50m,
            TotalUnits = list.Sum(g => g.Units)
        };

        return Task.FromResult(new List<TermGradeGroup> { group });
    }

    public Task<int> SubmitGradeCompletionAsync(int studentId, int gradeId, string reqType, decimal grade, string reason)
    {
        return Task.FromResult(1);
    }

    public Task<List<CompletionRevisionRecord>> GetGradeCompletionRequestsAsync(int studentId)
    {
        return Task.FromResult(new List<CompletionRevisionRecord>());
    }

    // ─── Enrollment & Registration ───────────────────────────────────────────

    public Task<StudentEnrollmentOverview?> GetActiveEnrollmentOverviewAsync(int studentId, string? schoolYear = null, string? semester = null)
    {
        return GetStudentEnrollmentAsync(studentId, schoolYear, semester);
    }

    public Task<StudentEnrollmentOverview?> GetStudentEnrollmentAsync(int studentId, string? schoolYear = null, string? semester = null)
    {
        var sy = schoolYear ?? "2026-2027";
        var sem = semester ?? "1st Semester";

        var enr = _jsonDb.GetEnrollments().FirstOrDefault(e => e.StudentId == studentId && e.SchoolYear == sy && e.Semester == sem);
        if (enr == null) return Task.FromResult<StudentEnrollmentOverview?>(null);

        var subjects = GetEnrolledClassesAsync(studentId, sy, sem).Result;

        var overview = new StudentEnrollmentOverview
        {
            Id = enr.Id,
            StudentId = enr.StudentId,
            SchoolYear = enr.SchoolYear,
            Semester = enr.Semester,
            EnrollmentType = enr.EnrollmentType,
            Status = enr.Status,
            EnrolledAt = enr.CreatedAt,
            TotalUnits = enr.TotalUnits,
            SubjectCount = subjects.Count,
            Subjects = subjects
        };

        return Task.FromResult<StudentEnrollmentOverview?>(overview);
    }

    public Task<List<StudentEnrollmentOverview>> GetEnrollmentHistoryAsync(int studentId)
    {
        var enrs = _jsonDb.GetEnrollments().Where(e => e.StudentId == studentId).ToList();
        var list = enrs.Select(enr => new StudentEnrollmentOverview
        {
            Id = enr.Id,
            StudentId = enr.StudentId,
            SchoolYear = enr.SchoolYear,
            Semester = enr.Semester,
            EnrollmentType = enr.EnrollmentType,
            Status = enr.Status,
            EnrolledAt = enr.CreatedAt,
            TotalUnits = enr.TotalUnits,
            SubjectCount = 7
        }).ToList();

        return Task.FromResult(list);
    }

    public Task<int> SubmitEnrollmentReservationAsync(int studentId, string sy, string sem, List<int> selectedOfferingIds)
    {
        var enr = _jsonDb.SubmitEnrollmentReservation(studentId, selectedOfferingIds, sy, sem);
        return Task.FromResult(enr.Id);
    }

    public Task<int> SubmitEnrollmentReservationAsync(int studentId, List<int> selectedOfferingIds, string sy = "2026-2027", string sem = "1st Semester")
    {
        return SubmitEnrollmentReservationAsync(studentId, sy, sem, selectedOfferingIds);
    }

    public Task SubmitCourseShiftingRequestAsync(int studentId, string fromProg, string toProg, string reason)
    {
        return Task.CompletedTask;
    }

    public Task<List<CourseShiftingRecord>> GetCourseShiftingRequestsAsync(int studentId)
    {
        return Task.FromResult(new List<CourseShiftingRecord>());
    }

    // ─── Clearance Records ───────────────────────────────────────────────────

    public Task<List<ClearanceRecord>> GetClearanceRecordsAsync(int studentId, string? schoolYear = null, string? semester = null)
    {
        var clrs = _jsonDb.GetStudentClearances().Where(c => c.StudentId == studentId).ToList();
        var list = clrs.Select(c => new ClearanceRecord
        {
            Id = c.Id,
            StudentId = c.StudentId,
            DepartmentName = c.DepartmentName,
            SchoolYear = c.SchoolYear,
            Semester = c.Semester,
            Status = c.Status,
            Remarks = c.Remarks,
            ClearedAt = c.UpdatedAt,
            ClearedBy = "Officer"
        }).ToList();

        return Task.FromResult(list);
    }

    // ─── Financial Assessments & Payments ────────────────────────────────────

    public Task<StudentFinancialAssessment> GetFinancialAssessmentAsync(int studentId)
    {
        var asm = _jsonDb.GetFeeAssessments().FirstOrDefault(a => a.StudentId == studentId);
        var payments = _jsonDb.GetStudentPayments().Where(p => p.StudentId == studentId).ToList();
        decimal totalPaid = payments.Sum(p => p.Amount);

        var result = new StudentFinancialAssessment
        {
            StudentId = studentId,
            TotalUnits = asm?.TotalUnits ?? 21,
            AssessmentNumber = asm?.AssessmentNumber ?? $"ASM-2026-{studentId:D5}",
            TotalPaid = totalPaid
        };

        return Task.FromResult(result);
    }

    public Task<List<PaymentRecord>> GetPaymentsAsync(int studentId)
    {
        var pays = _jsonDb.GetStudentPayments().Where(p => p.StudentId == studentId).ToList();
        var list = pays.Select(p => new PaymentRecord
        {
            Id = p.Id,
            StudentId = p.StudentId,
            ReceiptNo = p.ReceiptNo,
            Amount = p.Amount,
            PayDate = p.PayDate,
            PaymentMethod = p.PaymentMethod,
            Status = "Paid",
            Description = $"Tuition Payment ({p.PaymentMethod})"
        }).ToList();

        return Task.FromResult(list);
    }

    public Task<string> SubmitPaymentAsync(int studentId, decimal amount, string method, string desc)
    {
        var pay = _jsonDb.RecordPayment(studentId, amount, method, desc);
        return Task.FromResult(pay.ReceiptNo);
    }

    // ─── Document Requests (TOR, Good Moral, Certs) ──────────────────────────

    public Task<List<DocumentRequestRecord>> GetDocumentRequestsAsync(int studentId)
    {
        var reqs = _jsonDb.GetTranscriptRequests().Where(r => r.StudentId == studentId).ToList();
        var list = reqs.Select(r => new DocumentRequestRecord
        {
            Id = r.Id,
            StudentId = r.StudentId,
            DocumentType = r.DocumentType,
            Purpose = r.Purpose,
            Copies = r.Copies,
            Status = r.Status,
            QrCodeToken = r.QrCodeToken,
            Remarks = r.Remarks,
            RequestedAt = r.RequestedAt,
            ReleasedAt = r.ReleasedAt
        }).ToList();

        return Task.FromResult(list);
    }

    public Task<string> SubmitDocumentRequestAsync(int studentId, string docType, string purpose, int copies)
    {
        var req = _jsonDb.SubmitTranscriptRequest(studentId, docType, purpose, copies);
        return Task.FromResult(req.QrCodeToken);
    }

    public Task<List<DocumentCredentialItem>> GetDocumentVaultAsync(int studentId)
    {
        var list = VaultDocumentTypes.Select((doc, i) => new DocumentCredentialItem
        {
            Id = i + 1,
            StudentId = studentId,
            DocumentType = doc,
            Status = i < 4 ? "Verified" : "Submitted",
            Remarks = "Archived in 201 Credential Vault",
            SubmittedAt = DateTime.UtcNow.AddMonths(-3),
            VerifiedAt = DateTime.UtcNow.AddMonths(-2)
        }).ToList();

        return Task.FromResult(list);
    }

    public Task SubmitVaultFileAsync(int credentialId, int studentId, string fileName)
    {
        return Task.CompletedTask;
    }

    // ─── Add/Drop & Overload Petitions ───────────────────────────────────────

    public Task<List<AddDropRecord>> GetAddDropRequestsAsync(int studentId)
    {
        var reqs = _jsonDb.GetAddDropRequests().Where(r => r.StudentId == studentId).ToList();
        var list = reqs.Select(r => new AddDropRecord
        {
            Id = r.Id,
            StudentId = r.StudentId,
            RequestType = r.RequestType,
            Reason = r.Reason,
            Status = r.Status,
            CreatedAt = r.RequestedAt
        }).ToList();

        return Task.FromResult(list);
    }

    public Task<int> SubmitAddDropRequestAsync(int studentId, int enrollmentId, string reqType, int? currOffId, int? targetOffId, string reason)
    {
        var reqs = _jsonDb.GetAddDropRequests();
        int nextId = (reqs.MaxBy(r => r.Id)?.Id ?? 0) + 1;
        var users = _jsonDb.GetUsers();
        var u = users.FirstOrDefault(x => x.Id == studentId);

        reqs.Add(new AddDropRequestRecord
        {
            Id = nextId,
            StudentId = studentId,
            StudentNumber = u?.StudentIdNumber ?? $"2024-{studentId:D5}",
            StudentName = u?.Name ?? "Student",
            RequestType = reqType,
            CurrentOfferingId = currOffId,
            TargetOfferingId = targetOffId,
            Reason = reason ?? "",
            Status = "pending",
            RequestedAt = DateTime.UtcNow
        });

        _jsonDb.SaveAddDropRequests(reqs);
        return Task.FromResult(nextId);
    }

    public Task<List<OverloadRecord>> GetOverloadRequestsAsync(int studentId)
    {
        var reqs = _jsonDb.GetOverloadRequests().Where(r => r.StudentId == studentId).ToList();
        var list = reqs.Select(r => new OverloadRecord
        {
            Id = r.Id,
            StudentId = r.StudentId,
            SchoolYear = "2026-2027",
            Semester = "1st Semester",
            RequestType = r.RequestType,
            RequestedUnits = r.RequestedUnits,
            Reason = r.Reason,
            Status = r.Status,
            RequestedAt = r.RequestedAt
        }).ToList();

        return Task.FromResult(list);
    }

    public Task<int> SubmitOverloadRequestAsync(int studentId, string sy, string sem, string reqType, int units, string reason)
    {
        var reqs = _jsonDb.GetOverloadRequests();
        int nextId = (reqs.MaxBy(r => r.Id)?.Id ?? 0) + 1;
        var users = _jsonDb.GetUsers();
        var u = users.FirstOrDefault(x => x.Id == studentId);

        reqs.Add(new OverloadRequestRecord
        {
            Id = nextId,
            StudentId = studentId,
            StudentNumber = u?.StudentIdNumber ?? $"2024-{studentId:D5}",
            StudentName = u?.Name ?? "Student",
            RequestType = reqType,
            RequestedUnits = units,
            Reason = reason,
            Status = "pending",
            RequestedAt = DateTime.UtcNow
        });

        _jsonDb.SaveOverloadRequests(reqs);
        return Task.FromResult(nextId);
    }

    // ─── Assignments & Feedback ──────────────────────────────────────────────

    public Task<List<AssignmentItem>> GetAssignmentsAsync(int studentId)
    {
        return GetStudentAssignmentsAsync(studentId);
    }

    public Task SubmitAssignmentWorkAsync(int assignmentId, int studentId)
    {
        var assigns = _jsonDb.GetStudentAssignments();
        var item = assigns.FirstOrDefault(a => a.Id == assignmentId && a.StudentId == studentId);
        if (item != null)
        {
            item.IsSubmitted = true;
            item.SubmittedAt = DateTime.UtcNow;
            _jsonDb.SaveStudentAssignments(assigns);
        }
        return Task.CompletedTask;
    }

    public Task<List<AssignmentItem>> GetStudentAssignmentsAsync(int studentId)
    {
        var assigns = _jsonDb.GetStudentAssignments().Where(a => a.StudentId == studentId).ToList();
        var list = assigns.Select(a => new AssignmentItem
        {
            Id = a.Id,
            ClassOfferingId = a.ClassOfferingId,
            SubjectCode = a.SubjectCode,
            SubjectName = a.SubjectName,
            Title = a.Title,
            Description = a.Description,
            MaxScore = a.MaxScore,
            DueDate = a.DueDate,
            IsSubmitted = a.IsSubmitted,
            MyScore = a.MyScore,
            Status = a.IsSubmitted ? "Submitted" : "Assigned"
        }).ToList();

        return Task.FromResult(list);
    }

    public Task<List<StudentFeedbackItem>> GetStudentFeedbackAsync(int studentId)
    {
        var fb = _jsonDb.GetStudentFeedback().Where(f => f.StudentId == studentId).ToList();
        return Task.FromResult(fb);
    }

    // ─── Library & Scholarships ──────────────────────────────────────────────

    public Task<List<LibraryBookItem>> GetLibraryBooksAsync(string? search = null)
    {
        var books = new List<LibraryBookItem>
        {
            new() { Id = 1, Title = "Clean Architecture: A Craftsman's Guide", Author = "Robert C. Martin", Isbn = "978-0134494166", AvailableCopies = 4, TotalCopies = 5 },
            new() { Id = 2, Title = "Designing Data-Intensive Applications", Author = "Martin Kleppmann", Isbn = "978-1449373320", AvailableCopies = 2, TotalCopies = 3 },
            new() { Id = 3, Title = "Computer Networking: A Top-Down Approach", Author = "James Kurose", Isbn = "978-0133594140", AvailableCopies = 5, TotalCopies = 5 }
        };
        return Task.FromResult(books);
    }

    public Task<bool> SubmitLibraryReservationAsync(int studentId, string bookTitle, string bookAuthor)
    {
        return Task.FromResult(true);
    }

    public Task<List<LibraryReservationRecord>> GetLibraryReservationsAsync(int studentId)
    {
        return Task.FromResult(new List<LibraryReservationRecord>());
    }

    public List<ScholarshipItem> GetAvailableScholarships()
    {
        return
        [
            new() { Id = 1, Name = "President's Academic Excellence Scholarship", Description = "100% Tuition Discount for students with GWA 1.20 or better", SlotsAvailable = 10 },
            new() { Id = 2, Name = "Dean's Lister Study Grant", Description = "50% Tuition Discount for Semester Top Rankers", SlotsAvailable = 25 }
        ];
    }

    public Task<List<ScholarshipItem>> GetAvailableScholarshipsAsync()
    {
        return Task.FromResult(GetAvailableScholarships());
    }

    public Task<bool> SubmitScholarshipApplicationAsync(int studentId, string scholarshipName, string sy, string sem)
    {
        return Task.FromResult(true);
    }

    public Task<List<ScholarshipApplicationRecord>> GetScholarshipApplicationsAsync(int studentId)
    {
        return Task.FromResult(new List<ScholarshipApplicationRecord>());
    }
}
