using FacultyPortalMain.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FacultyPortalMain.Pages;

public class FeedbackModel : PageModel
{
    private readonly FacultyDbService _db;

    public FeedbackModel(FacultyDbService db)
    {
        _db = db;
    }

    public List<FeedbackItemDto> RecentFeedback { get; set; } = new();
    public List<FacultyDbService.ClassOfferingItem> Offerings { get; set; } = new();
    public List<FacultyDbService.StudentRosterItem> Students { get; set; } = new();
    public List<AssignmentSummaryDto> Assignments { get; set; } = new();

    [TempData]
    public string? Message { get; set; }

    public async Task OnGetAsync(int? offeringId)
    {
        RecentFeedback = await _db.GetRecentFeedbackAsync();
        Offerings = await _db.GetClassOfferingsAsync();
        Assignments = await _db.GetFacultyAssignmentsAsync();

        var selectedOffId = offeringId ?? Offerings.FirstOrDefault()?.Id ?? 0;
        if (selectedOffId > 0)
        {
            Students = await _db.GetClassRosterAsync(selectedOffId);
        }
    }

    public async Task<IActionResult> OnPostAsync(int studentId, string assignmentTitle, string comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            Message = "Feedback comment cannot be empty.";
            return RedirectToPage();
        }

        var ok = await _db.SaveFeedbackAsync(studentId, assignmentTitle, comment, "Prof. Torres");
        Message = ok ? "Feedback recorded and sent to student record in MySQL." : "Failed to save feedback.";

        return RedirectToPage();
    }
}
