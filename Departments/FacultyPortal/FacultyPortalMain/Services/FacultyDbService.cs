using CampusSystem.Data.Services;
using CampusSystem.Data.Models;

namespace FacultyPortalMain.Services;

public class FacultyDbService
{
    private readonly CampusJsonDb _jsonDb;
    private readonly ILogger<FacultyDbService> _logger;

    public FacultyDbService(IConfiguration config, ILogger<FacultyDbService> logger, CampusJsonDb jsonDb)
    {
<<<<<<< Updated upstream
        _connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is required.");
=======
>>>>>>> Stashed changes
        _logger = logger;
        _jsonDb = jsonDb;
    }

    // ─── Inner DTOs (kept identical so Razor Pages compile unchanged) ──────────

    public class ClassOfferingItem
    {
        public int Id { get; set; }
        public string SectionCode { get; set; } = "";
        public string SubjectCode { get; set; } = "";
        public string SubjectName { get; set; } = "";
        public int Units { get; set; }
        public string Room { get; set; } = "";
        public string DaysOfWeek { get; set; } = "";
        public string StartTime { get; set; } = "";
        public string EndTime { get; set; } = "";
        public string InstructorName { get; set; } = "";
        public int EnrolledCount { get; set; }
    }

    public class StudentRosterItem
    {
        public int StudentId { get; set; }
        public string StudentIdNumber { get; set; } = "";
        public string Name { get; set; } = "";
        public string Program { get; set; } = "";
        public string YearLevel { get; set; } = "";
        public string FinanceClearanceStatus { get; set; } = "Cleared";
        public decimal? FinalGrade { get; set; }
        public bool IsInc { get; set; }
        public string Remarks { get; set; } = "";
        public int EnrolledSubjectId { get; set; }
    }

    // ─── Class Offerings ───────────────────────────────────────────────────────

    /// <summary>
    /// Returns all active class offerings from the shared JSON store.
    /// </summary>
    public Task<List<ClassOfferingItem>> GetClassOfferingsAsync(
        string? instructor = null,
        string schoolYear = "2025-2026",
        string semester = "1st Semester")
    {
        try
        {
            var offerings = _jsonDb.GetClassOfferings();
            var subjects  = _jsonDb.GetSubjects();
            var enrolledSubjects = _jsonDb.GetEnrolledSubjects();

            var query = offerings.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(instructor))
                query = query.Where(o => o.InstructorName.Contains(instructor, StringComparison.OrdinalIgnoreCase));

            var list = query.Select(o =>
            {
                var subj = subjects.FirstOrDefault(s => s.Id == o.SubjectId);
                int count = enrolledSubjects.Count(es => es.ClassOfferingId == o.Id);
                return new ClassOfferingItem
                {
                    Id           = o.Id,
                    SectionCode  = o.SectionCode,
                    SubjectCode  = subj?.Code ?? o.SubjectCode,
                    SubjectName  = subj?.Title ?? o.SubjectTitle,
                    Units        = subj?.Units ?? o.Units,
                    Room         = o.Room,
                    DaysOfWeek   = o.DaysOfWeek,
                    StartTime    = o.StartTime,
                    EndTime      = o.EndTime,
                    InstructorName = o.InstructorName,
                    EnrolledCount  = count
                };
            }).ToList();

            return Task.FromResult(list);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to read class offerings from JSON: {Message}", ex.Message);
            return Task.FromResult(new List<ClassOfferingItem>());
        }
    }

    // ─── Class Roster ──────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all enrolled students for a specific class offering,
    /// with Finance clearance status and grades from shared JSON.
    /// </summary>
    public Task<List<StudentRosterItem>> GetClassRosterAsync(int classOfferingId)
    {
        try
        {
            var users            = _jsonDb.GetUsers().Where(u => u.Role == "student").ToList();
            var profiles         = _jsonDb.GetStudentProfiles();
            var enrollments      = _jsonDb.GetEnrollments();
            var enrolledSubjects = _jsonDb.GetEnrolledSubjects();
            var clearances       = _jsonDb.GetStudentClearances();
            var grades           = _jsonDb.GetGrades();

            // Find enrolled_subjects that belong to this class offering
            var esInClass = enrolledSubjects
                .Where(es => es.ClassOfferingId == classOfferingId)
                .ToList();

            List<StudentRosterItem> list;

            if (esInClass.Count > 0)
            {
                list = esInClass.Select(es =>
                {
                    var enrollment = enrollments.FirstOrDefault(e => e.Id == es.EnrollmentId);
                    var u   = users.FirstOrDefault(u => u.Id == (enrollment?.StudentId ?? 0));
                    var prof = profiles.FirstOrDefault(p => p.StudentId == (u?.Id ?? 0));
                    var clr = clearances.FirstOrDefault(c =>
                        c.StudentId == (u?.Id ?? 0) &&
                        (c.DepartmentName.Contains("Finance", StringComparison.OrdinalIgnoreCase)));
                    var grade = grades.FirstOrDefault(g => g.ClassOfferingId == classOfferingId && g.StudentId == (u?.Id ?? 0));

                    decimal? parsedFinalGrade = null;
                    if (grade != null && decimal.TryParse(grade.FinalGrade, out var fg))
                    {
                        parsedFinalGrade = fg;
                    }

                    return new StudentRosterItem
                    {
                        StudentId             = u?.Id ?? 0,
                        StudentIdNumber       = u?.StudentIdNumber ?? "",
                        Name                  = u?.Name ?? "Unknown Student",
                        Program               = prof?.Program ?? "BS Information Technology",
                        YearLevel             = prof != null ? $"{prof.YearLevel}th Year" : "3rd Year",
                        FinanceClearanceStatus = clr?.Status ?? "Cleared",
                        FinalGrade            = parsedFinalGrade,
                        IsInc                 = grade?.Remarks == "Incomplete" || grade?.FinalGrade == "INC",
                        Remarks               = grade?.Remarks ?? "",
                        EnrolledSubjectId     = es.Id
                    };
                }).OrderBy(r => r.Name).ToList();
            }
            else
            {
                // Fallback: show all active students as roster preview
                list = users.Select(u =>
                {
                    var prof = profiles.FirstOrDefault(p => p.StudentId == u.Id);
                    var clr  = clearances.FirstOrDefault(c =>
                        c.StudentId == u.Id &&
                        c.DepartmentName.Contains("Finance", StringComparison.OrdinalIgnoreCase));
                    return new StudentRosterItem
                    {
                        StudentId             = u.Id,
                        StudentIdNumber       = u.StudentIdNumber,
                        Name                  = u.Name,
                        Program               = prof?.Program ?? "BS Information Technology",
                        YearLevel             = prof != null ? $"{prof.YearLevel}th Year" : "3rd Year",
                        FinanceClearanceStatus = clr?.Status ?? "Cleared",
                        FinalGrade            = null,
                        IsInc                 = false,
                        Remarks               = "",
                        EnrolledSubjectId     = 0
                    };
                }).OrderBy(r => r.Name).Take(8).ToList();
            }

            return Task.FromResult(list);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to read class roster from JSON: {Message}", ex.Message);
            return Task.FromResult(new List<StudentRosterItem>());
        }
    }

    // ─── Grades ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Saves a student grade to the shared JSON store.
    /// Automatically triggers a Guidance referral if grade is 5.00 or INC.
    /// </summary>
    public Task<bool> SaveGradeAsync(int enrolledSubjectId, decimal grade, string remarks)
    {
        try
        {
            var enrolledSubjects = _jsonDb.GetEnrolledSubjects();
            var es = enrolledSubjects.FirstOrDefault(e => e.Id == enrolledSubjectId);
            if (es == null)
            {
                _logger.LogWarning("EnrolledSubjectId {Id} not found in JSON", enrolledSubjectId);
                return Task.FromResult(false);
            }

            var enrollments = _jsonDb.GetEnrollments();
            var enrollment  = enrollments.FirstOrDefault(e => e.Id == es.EnrollmentId);
            int studentId   = enrollment?.StudentId ?? 0;

            var offerings = _jsonDb.GetClassOfferings();
            var offering  = offerings.FirstOrDefault(o => o.Id == es.ClassOfferingId);
            string subjectCode = offering?.SubjectCode ?? "SUBJ";
            string subjectTitle = offering?.SubjectTitle ?? "Subject";

            bool isInc = string.Equals(remarks, "INC", StringComparison.OrdinalIgnoreCase)
                      || string.Equals(remarks, "Incomplete", StringComparison.OrdinalIgnoreCase);

            string finalGradeStr = isInc ? "INC" : grade.ToString("0.00");

            // Use CampusJsonDb.SubmitGrade — auto-creates Guidance referral for 5.00/INC
            _jsonDb.SubmitGrade(
                studentId:        studentId,
                classOfferingId:  es.ClassOfferingId,
                subjectCode:      subjectCode,
                subjectTitle:     subjectTitle,
                prelim:           null,
                midterm:          null,
                finals:           grade,
                finalGrade:       finalGradeStr,
                facultyName:      "Faculty");

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to save grade to JSON: {Message}", ex.Message);
            return Task.FromResult(false);
        }
    }

    // ─── Attendance ────────────────────────────────────────────────────────────

    public Task EnsureAttendanceSchemaAsync() => Task.CompletedTask; // No-op — JSON needs no schema

    public async Task<List<AttendanceRecordDto>> GetAttendanceSheetAsync(int classOfferingId, DateTime sessionDate)
    {
        try
        {
            var roster = await GetClassRosterAsync(classOfferingId);
            var records = _jsonDb.GetAttendanceRecords();

            var dateStr = sessionDate.Date;
            var existingByStudent = records
                .Where(r => r.ClassOfferingId == classOfferingId && r.SessionDate.Date == dateStr)
                .ToDictionary(r => r.StudentId, r => r.Status);

            return roster.Select(r => new AttendanceRecordDto
            {
                StudentId     = r.StudentId,
                StudentNumber = r.StudentIdNumber,
                StudentName   = r.Name,
                Status        = existingByStudent.TryGetValue(r.StudentId, out var st) ? st : "Present"
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to read attendance sheet from JSON: {Message}", ex.Message);
            return new List<AttendanceRecordDto>();
        }
    }

    public Task<bool> SaveAttendanceBatchAsync(
        int classOfferingId,
        DateTime sessionDate,
        string? topic,
        List<(int StudentId, string Status)> items)
    {
        try
        {
            var records = _jsonDb.GetAttendanceRecords();
            var dateOnly = sessionDate.Date;

            foreach (var (studentId, status) in items)
            {
                var existing = records.FirstOrDefault(r =>
                    r.ClassOfferingId == classOfferingId &&
                    r.StudentId == studentId &&
                    r.SessionDate.Date == dateOnly);

                if (existing != null)
                {
                    existing.Status = status;
                    existing.Topic  = topic ?? "";
                }
                else
                {
                    int newId = records.Count > 0 ? records.Max(r => r.Id) + 1 : 1;
                    records.Add(new AttendanceRecordItem
                    {
                        Id              = newId,
                        ClassOfferingId = classOfferingId,
                        StudentId       = studentId,
                        SessionDate     = sessionDate,
                        Topic           = topic ?? "",
                        Status          = status
                    });
                }
            }

            _jsonDb.SaveAttendanceRecords(records);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to save attendance batch to JSON: {Message}", ex.Message);
            return Task.FromResult(false);
        }
    }

    // ─── Assignments ───────────────────────────────────────────────────────────

    public Task<List<AssignmentSummaryDto>> GetFacultyAssignmentsAsync()
    {
        try
        {
            var assignments = _jsonDb.GetStudentAssignments();

            var grouped = assignments
                .GroupBy(a => new { a.SubjectCode, a.SubjectName, a.Title, a.Description })
                .Select(g => new AssignmentSummaryDto
                {
                    SubjectCode       = g.Key.SubjectCode,
                    SubjectName       = g.Key.SubjectName,
                    Title             = g.Key.Title,
                    Description       = g.Key.Description,
                    MaxScore          = g.Max(a => a.MaxScore),
                    DueDate           = g.Max(a => a.DueDate),
                    TotalAssigned     = g.Count(),
                    SubmissionsCount  = g.Count(a => a.IsSubmitted),
                    GradedCount       = g.Count(a => a.MyScore.HasValue)
                })
                .OrderByDescending(a => a.DueDate)
                .ToList();

            return Task.FromResult(grouped);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to read assignments from JSON: {Message}", ex.Message);
            return Task.FromResult(new List<AssignmentSummaryDto>());
        }
    }

    public async Task<bool> CreateAssignmentAsync(
        int classOfferingId,
        string title,
        string description,
        decimal maxScore,
        DateTime dueDate)
    {
        try
        {
            var roster   = await GetClassRosterAsync(classOfferingId);
            var offerings = _jsonDb.GetClassOfferings();
            var off      = offerings.FirstOrDefault(o => o.Id == classOfferingId);
            string subjCode = off?.SubjectCode ?? "IT104";
            string subjTitle = off?.SubjectTitle ?? "Computer Studies";

            var assignments = _jsonDb.GetStudentAssignments();
            int nextId = assignments.Count > 0 ? assignments.Max(a => a.Id) + 1 : 1;

            foreach (var student in roster)
            {
                assignments.Add(new StudentAssignmentItem
                {
                    Id              = nextId++,
                    ClassOfferingId = classOfferingId,
                    StudentId       = student.StudentId,
                    SubjectCode     = subjCode,
                    SubjectName     = subjTitle,
                    Title           = title,
                    Description     = description,
                    MaxScore        = (int)maxScore,
                    DueDate         = dueDate,
                    IsSubmitted     = false,
                    MyScore         = null
                });
            }

            _jsonDb.SaveStudentAssignments(assignments);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to create assignment in JSON: {Message}", ex.Message);
            return false;
        }
    }

    // ─── Feedback ──────────────────────────────────────────────────────────────

    public Task EnsureFeedbackSchemaAsync() => Task.CompletedTask; // No-op — JSON needs no schema

    public Task<List<FeedbackItemDto>> GetRecentFeedbackAsync()
    {
        try
        {
            var feedback = _jsonDb.GetStudentFeedback();
            var users    = _jsonDb.GetUsers();

            var list = feedback
                .OrderByDescending(f => f.CreatedAt)
                .Take(25)
                .Select(f =>
                {
                    var u = users.FirstOrDefault(u => u.Id == f.StudentId);
                    return new FeedbackItemDto
                    {
                        Id              = f.Id,
                        StudentName     = u?.Name ?? "Student",
                        AssignmentTitle = f.AssignmentTitle,
                        Comment         = f.Comment,
                        CreatedAt       = f.CreatedAt
                    };
                }).ToList();

            return Task.FromResult(list);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to read feedback from JSON: {Message}", ex.Message);
            return Task.FromResult(new List<FeedbackItemDto>());
        }
    }

    public Task<bool> SaveFeedbackAsync(int studentId, string assignmentTitle, string comment, string facultyName)
    {
        try
        {
            var feedback = _jsonDb.GetStudentFeedback();
            int nextId   = feedback.Count > 0 ? feedback.Max(f => f.Id) + 1 : 1;

            feedback.Add(new StudentFeedbackItem
            {
                Id              = nextId,
                ClassOfferingId = 0,
                FacultyName     = facultyName,
                StudentId       = studentId,
                AssignmentTitle = assignmentTitle,
                Comment         = comment,
                CreatedAt       = DateTime.UtcNow
            });

            _jsonDb.SaveStudentFeedback(feedback);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to save feedback to JSON: {Message}", ex.Message);
            return Task.FromResult(false);
        }
    }
}

// ─── DTOs (kept identical for Razor Page compatibility) ───────────────────────

public class AttendanceRecordDto
{
    public int StudentId { get; set; }
    public string StudentNumber { get; set; } = "";
    public string StudentName { get; set; } = "";
    public string Status { get; set; } = "Present";
}

public class AssignmentSummaryDto
{
    public string Title { get; set; } = "";
    public string SubjectCode { get; set; } = "";
    public string SubjectName { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal MaxScore { get; set; }
    public DateTime DueDate { get; set; }
    public int TotalAssigned { get; set; }
    public int SubmissionsCount { get; set; }
    public int GradedCount { get; set; }
}

public class FeedbackItemDto
{
    public int Id { get; set; }
    public string StudentName { get; set; } = "";
    public string AssignmentTitle { get; set; } = "";
    public string Comment { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
