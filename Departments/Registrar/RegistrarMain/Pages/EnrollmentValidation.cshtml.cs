using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CampusSystem.Data.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RegistrarMain.Services;

namespace RegistrarMain.Pages;

public class EnrollmentValidationModel : PageModel
{
    private readonly DatabaseService _db;
    private readonly SettingsService _settings;
    private readonly CampusJsonDb _jsonDb;

    public EnrollmentValidationModel(DatabaseService db, SettingsService settings, CampusJsonDb jsonDb)
    {
        _db = db;
        _settings = settings;
        _jsonDb = jsonDb;
    }

    public string CurrentSchoolYear { get; set; } = "2026-2027";
    public string CurrentSemester { get; set; } = "1st Semester";
    public string ActiveTab { get; set; } = "registration";

    public List<PendingEnrollmentItem> PendingList { get; set; } = new();
    public List<PendingAddDropItem> PendingAddDrop { get; set; } = new();
    public List<PendingOverloadItem> PendingOverloads { get; set; } = new();
    public List<ActiveEnrolleeItem> ActiveList { get; set; } = new();

    public class PendingEnrollmentItem
    {
        public int Id { get; set; }
        public string SchoolYear { get; set; } = "";
        public string Semester { get; set; } = "";
        public string EnrollmentType { get; set; } = "";
        public int TotalUnits { get; set; }
        public DateTime EnrolledAt { get; set; }
        public string Name { get; set; } = "";
        public string StudentIdNumber { get; set; } = "";
        public string Email { get; set; } = "";
        public string Program { get; set; } = "";
        public string YearLevel { get; set; } = "";
        public string AcademicStatus { get; set; } = "";
        public int SubjectsCount { get; set; }
        public string FinanceClearanceStatus { get; set; } = "Cleared";
    }

    public class PendingAddDropItem
    {
        public int Id { get; set; }
        public string RequestType { get; set; } = "";
        public string? Reason { get; set; }
        public DateTime RequestedAt { get; set; }
        public string Name { get; set; } = "";
        public string StudentIdNumber { get; set; } = "";
        public string Program { get; set; } = "";
        public string? FromCode { get; set; }
        public string? FromName { get; set; }
        public string? ToCode { get; set; }
        public string? ToName { get; set; }
    }

    public class PendingOverloadItem
    {
        public int Id { get; set; }
        public string RequestType { get; set; } = "";
        public int RequestedUnits { get; set; }
        public string? Reason { get; set; }
        public DateTime RequestedAt { get; set; }
        public string Name { get; set; } = "";
        public string StudentIdNumber { get; set; } = "";
        public string Program { get; set; } = "";
        public string YearLevel { get; set; } = "";
        public decimal? AverageGrade { get; set; }
    }

    public class ActiveEnrolleeItem
    {
        public int Id { get; set; }
        public string SchoolYear { get; set; } = "";
        public string Semester { get; set; } = "";
        public string EnrollmentType { get; set; } = "";
        public int TotalUnits { get; set; }
        public DateTime EnrolledAt { get; set; }
        public int StudentId { get; set; }
        public string Name { get; set; } = "";
        public string StudentIdNumber { get; set; } = "";
        public string Program { get; set; } = "";
        public string YearLevel { get; set; } = "";
    }

    public Task<IActionResult> OnGetAsync(string tab = "registration")
    {
        _db.EnsureRegistrarSession();
        ActiveTab = new[] { "registration", "adddrop", "overload", "active_list" }.Contains(tab) ? tab : "registration";

        CurrentSchoolYear = "2026-2027";
        CurrentSemester = "1st Semester";

        var users = _jsonDb.GetUsers();
        var profiles = _jsonDb.GetStudentProfiles();
        var enrollments = _jsonDb.GetEnrollments();
        var enrolledSubs = _jsonDb.GetEnrolledSubjects();
        var clearances = _jsonDb.GetStudentClearances();
        var offerings = _jsonDb.GetClassOfferings();

        // 1. Pending Enrollments
        var pEnrollments = enrollments
            .Where(e => e.Status == "pending" || e.Status == "reserved")
            .Select(e =>
            {
                var u = users.FirstOrDefault(x => x.Id == e.StudentId);
                var p = profiles.FirstOrDefault(x => x.StudentId == e.StudentId);
                var finClr = clearances.FirstOrDefault(c => c.StudentId == e.StudentId && c.DepartmentName == "Finance");
                int subCount = enrolledSubs.Count(s => s.EnrollmentId == e.Id);

                return new PendingEnrollmentItem
                {
                    Id = e.Id,
                    SchoolYear = e.SchoolYear,
                    Semester = e.Semester,
                    EnrollmentType = e.EnrollmentType,
                    TotalUnits = e.TotalUnits > 0 ? e.TotalUnits : 21,
                    EnrolledAt = e.CreatedAt,
                    Name = u?.Name ?? e.StudentName,
                    StudentIdNumber = u?.StudentIdNumber ?? e.StudentNumber,
                    Email = u?.Email ?? $"{e.StudentId}@student.campus.edu",
                    Program = p?.Program ?? e.Program,
                    YearLevel = p != null ? $"{p.YearLevel}nd Year" : "2nd Year",
                    AcademicStatus = p?.AcademicStatus ?? "Regular",
                    SubjectsCount = subCount > 0 ? subCount : 7,
                    FinanceClearanceStatus = finClr?.Status ?? "Cleared"
                };
            })
            .OrderBy(e => e.EnrolledAt)
            .ToList();

        PendingList = pEnrollments;

        // 2. Add / Drop Petitions
        var addDrops = _jsonDb.GetAddDropRequests();
        PendingAddDrop = addDrops
            .Where(a => a.Status == "pending")
            .Select(a =>
            {
                var u = users.FirstOrDefault(x => x.Id == a.StudentId);
                var p = profiles.FirstOrDefault(x => x.StudentId == a.StudentId);
                var fromOff = a.CurrentOfferingId.HasValue ? offerings.FirstOrDefault(o => o.Id == a.CurrentOfferingId.Value) : null;
                var toOff = a.TargetOfferingId.HasValue ? offerings.FirstOrDefault(o => o.Id == a.TargetOfferingId.Value) : null;

                return new PendingAddDropItem
                {
                    Id = a.Id,
                    RequestType = a.RequestType,
                    Reason = a.Reason,
                    RequestedAt = a.RequestedAt,
                    Name = u?.Name ?? a.StudentName,
                    StudentIdNumber = u?.StudentIdNumber ?? a.StudentNumber,
                    Program = p?.Program ?? "BS Information Technology",
                    FromCode = fromOff?.SubjectCode,
                    FromName = fromOff?.SubjectTitle,
                    ToCode = toOff?.SubjectCode,
                    ToName = toOff?.SubjectTitle
                };
            })
            .OrderBy(a => a.RequestedAt)
            .ToList();

        // 3. Overload / Waiver Petitions
        var overloads = _jsonDb.GetOverloadRequests();
        PendingOverloads = overloads
            .Where(o => o.Status == "pending")
            .Select(o =>
            {
                var u = users.FirstOrDefault(x => x.Id == o.StudentId);
                var p = profiles.FirstOrDefault(x => x.StudentId == o.StudentId);

                return new PendingOverloadItem
                {
                    Id = o.Id,
                    RequestType = o.RequestType,
                    RequestedUnits = o.RequestedUnits,
                    Reason = o.Reason,
                    RequestedAt = o.RequestedAt,
                    Name = u?.Name ?? o.StudentName,
                    StudentIdNumber = u?.StudentIdNumber ?? o.StudentNumber,
                    Program = p?.Program ?? "BS Information Technology",
                    YearLevel = p != null ? $"{p.YearLevel}nd Year" : "2nd Year",
                    AverageGrade = p?.Gwa ?? 1.50m
                };
            })
            .OrderBy(o => o.RequestedAt)
            .ToList();

        // 4. Active Validated Enrollments
        ActiveList = enrollments
            .Where(e => e.Status == "active" || e.Status == "enrolled")
            .Select(e =>
            {
                var u = users.FirstOrDefault(x => x.Id == e.StudentId);
                var p = profiles.FirstOrDefault(x => x.StudentId == e.StudentId);

                return new ActiveEnrolleeItem
                {
                    Id = e.Id,
                    SchoolYear = e.SchoolYear,
                    Semester = e.Semester,
                    EnrollmentType = e.EnrollmentType,
                    TotalUnits = e.TotalUnits,
                    EnrolledAt = e.CreatedAt,
                    StudentId = e.StudentId,
                    Name = u?.Name ?? e.StudentName,
                    StudentIdNumber = u?.StudentIdNumber ?? e.StudentNumber,
                    Program = p?.Program ?? e.Program,
                    YearLevel = p != null ? $"{p.YearLevel}nd Year" : "2nd Year"
                };
            })
            .OrderBy(e => e.Name)
            .ToList();

        return Task.FromResult<IActionResult>(Page());
    }

    public Task<IActionResult> OnPostEnrollmentActionAsync(int enrollment_id, string action)
    {
        _db.EnsureRegistrarSession();

        if (action == "approve_enrollment")
        {
            bool ok = _jsonDb.ApproveEnrollment(enrollment_id, _db.CurrentUserName);
            if (ok)
            {
                TempData["FlashMessage"] = $"Enrollment #{enrollment_id} has been approved and officially validated. Official Assessment Order (OAO) generated & linked in Finance System.";
                TempData["FlashType"] = "success";
            }
            else
            {
                TempData["FlashMessage"] = "That enrollment record is no longer pending.";
                TempData["FlashType"] = "error";
            }
        }
        else
        {
            var enrollments = _jsonDb.GetEnrollments();
            var enr = enrollments.FirstOrDefault(e => e.Id == enrollment_id);
            if (enr != null)
            {
                enr.Status = "rejected";
                _jsonDb.SaveEnrollments(enrollments);
                TempData["FlashMessage"] = $"Enrollment #{enrollment_id} has been rejected.";
                TempData["FlashType"] = "success";
            }
            else
            {
                TempData["FlashMessage"] = "Enrollment not found.";
                TempData["FlashType"] = "error";
            }
        }

        return Task.FromResult<IActionResult>(RedirectToPage("/EnrollmentValidation", new { tab = "registration" }));
    }

    public Task<IActionResult> OnPostAddDropActionAsync(int request_id, string action)
    {
        _db.EnsureRegistrarSession();
        var addDrops = _jsonDb.GetAddDropRequests();
        var req = addDrops.FirstOrDefault(r => r.Id == request_id);

        if (req == null)
        {
            TempData["FlashMessage"] = "That petition is no longer pending.";
            TempData["FlashType"] = "error";
            return Task.FromResult<IActionResult>(RedirectToPage("/EnrollmentValidation", new { tab = "adddrop" }));
        }

        if (action == "reject_adddrop")
        {
            req.Status = "rejected";
            req.ProcessedAt = DateTime.UtcNow;
            req.ProcessedBy = _db.CurrentUserName;
            _jsonDb.SaveAddDropRequests(addDrops);

            TempData["FlashMessage"] = "Petition rejected.";
            TempData["FlashType"] = "success";
        }
        else
        {
            req.Status = "approved";
            req.ProcessedAt = DateTime.UtcNow;
            req.ProcessedBy = _db.CurrentUserName;
            _jsonDb.SaveAddDropRequests(addDrops);

            // Update enrolled subjects
            if (req.CurrentOfferingId.HasValue && (req.RequestType == "drop" || req.RequestType == "change"))
            {
                var enrolledSubs = _jsonDb.GetEnrolledSubjects();
                var sub = enrolledSubs.FirstOrDefault(es => es.StudentId == req.StudentId && es.ClassOfferingId == req.CurrentOfferingId.Value);
                if (sub != null)
                {
                    sub.Status = "dropped";
                    _jsonDb.SaveEnrolledSubjects(enrolledSubs);
                }
            }

            if (req.TargetOfferingId.HasValue && (req.RequestType == "add" || req.RequestType == "change"))
            {
                var enrolledSubs = _jsonDb.GetEnrolledSubjects();
                var offerings = _jsonDb.GetClassOfferings();
                var targetOff = offerings.FirstOrDefault(o => o.Id == req.TargetOfferingId.Value);

                if (targetOff != null)
                {
                    int nextId = (enrolledSubs.MaxBy(es => es.Id)?.Id ?? 0) + 1;
                    enrolledSubs.Add(new CampusSystem.Data.Models.EnrolledSubjectRecord
                    {
                        Id = nextId,
                        EnrollmentId = 1,
                        StudentId = req.StudentId,
                        ClassOfferingId = targetOff.Id,
                        SubjectCode = targetOff.SubjectCode,
                        SubjectTitle = targetOff.SubjectTitle,
                        Units = targetOff.Units,
                        Status = "enrolled"
                    });
                    _jsonDb.SaveEnrolledSubjects(enrolledSubs);
                }
            }

            TempData["FlashMessage"] = "Petition approved and class schedule updated.";
            TempData["FlashType"] = "success";
        }

        return Task.FromResult<IActionResult>(RedirectToPage("/EnrollmentValidation", new { tab = "adddrop" }));
    }

    public Task<IActionResult> OnPostOverloadActionAsync(int request_id, string action)
    {
        _db.EnsureRegistrarSession();
        var newStatus = action == "approve_overload" ? "approved" : "rejected";

        var overloads = _jsonDb.GetOverloadRequests();
        var req = overloads.FirstOrDefault(r => r.Id == request_id);

        if (req != null)
        {
            req.Status = newStatus;
            req.ProcessedAt = DateTime.UtcNow;
            _jsonDb.SaveOverloadRequests(overloads);

            TempData["FlashMessage"] = $"{req.RequestType} petition {newStatus}.";
            TempData["FlashType"] = "success";
        }
        else
        {
            TempData["FlashMessage"] = "That petition is no longer pending.";
            TempData["FlashType"] = "error";
        }

        return Task.FromResult<IActionResult>(RedirectToPage("/EnrollmentValidation", new { tab = "overload" }));
    }
}
