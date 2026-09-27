using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StudentPortalMain.Models;
using StudentPortalMain.Services;

namespace StudentPortalMain.Pages;

public class GradesModel : PageModel
{
    private readonly StudentPortalDbService _db;

    public GradesModel(StudentPortalDbService db)
    {
        _db = db;
    }

    public StudentUser? Student { get; set; }
    public StudentProfileData Profile { get; set; } = new();
    public List<TermGradeGroup> TermGrades { get; set; } = new();
    public decimal? CumulativeGwa { get; set; }
    public int TotalUnitsEarned { get; set; }
    public List<CompletionRevisionRecord> MyRevisionRequests { get; set; } = new();

    public async Task OnGetAsync()
    {
        var studentId = _db.CurrentStudentId;
        Student = await _db.GetStudentUserAsync(studentId);
        Profile = await _db.GetStudentProfileAsync(studentId);

        TermGrades = await _db.GetGradesGroupedByTermAsync(studentId);
        CumulativeGwa = await _db.CalculateCumulativeGwaAsync(studentId) ?? Profile.AverageGrade;

        TotalUnitsEarned = TermGrades
            .SelectMany(t => t.Grades)
            .Where(g => g.Grade.HasValue && g.Grade.Value <= 3.00m && !g.IsInc)
            .Sum(g => g.Units);

        // Fetch revision requests
        var conn = _db.Connection;
        var reqs = await Dapper.SqlMapper.QueryAsync<CompletionRevisionRecord>(conn,
            @"SELECT crr.id, crr.student_id AS StudentId, crr.grade_id AS GradeId, 
                     s.subject_code AS SubjectCode, s.subject_name AS SubjectName, 
                     crr.request_type AS RequestType, crr.requested_grade AS RequestedGrade, 
                     crr.reason AS Reason, crr.status AS Status, crr.requested_at AS RequestedAt 
              FROM completion_revision_requests crr 
              JOIN grades g ON g.id = crr.grade_id 
              JOIN enrolled_subjects es ON es.id = g.enrolled_subject_id 
              JOIN class_offerings co ON co.id = es.class_offering_id 
              JOIN subjects s ON s.id = co.subject_id 
              WHERE crr.student_id = @studentId 
              ORDER BY crr.id DESC",
            new { studentId });
        MyRevisionRequests = reqs.AsList();
    }

    public async Task<IActionResult> OnPostRequestRevisionAsync(int gradeId, string requestType, decimal requestedGrade, string reason)
    {
        var studentId = _db.CurrentStudentId;
        if (gradeId <= 0 || string.IsNullOrWhiteSpace(reason))
        {
            TempData["FlashMessage"] = "Please provide complete information for your grade request.";
            TempData["FlashType"] = "danger";
            return RedirectToPage();
        }

        var conn = _db.Connection;
        await Dapper.SqlMapper.ExecuteAsync(conn,
            @"INSERT INTO completion_revision_requests 
              (grade_id, student_id, request_type, requested_grade, reason, status, requested_at) 
              VALUES (@gradeId, @studentId, @requestType, @requestedGrade, @reason, 'pending', NOW())",
            new { gradeId, studentId, requestType, requestedGrade, reason });

        await _db.LogActivityAsync(studentId, $"Submitted {requestType.ToUpper()} petition for Grade ID #{gradeId} (Requested: {requestedGrade:0.00}) via Student Portal");

        TempData["FlashMessage"] = $"Your {requestType} request has been submitted to the Registrar for review.";
        TempData["FlashType"] = "success";
        return RedirectToPage();
    }
}
