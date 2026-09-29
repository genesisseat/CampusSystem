using FacultyPortalMain.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FacultyPortalMain.Pages;

public class AssignmentsModel : PageModel
{
    private readonly FacultyDbService _db;

    public AssignmentsModel(FacultyDbService db)
    {
        _db = db;
    }

    public List<AssignmentSummaryDto> Assignments { get; set; } = new();
    public List<FacultyDbService.ClassOfferingItem> Offerings { get; set; } = new();

    [TempData]
    public string? Message { get; set; }

    public async Task OnGetAsync()
    {
        Assignments = await _db.GetFacultyAssignmentsAsync();
        Offerings = await _db.GetClassOfferingsAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync(int classOfferingId, string title, string description, decimal maxScore, DateTime dueDate)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            Message = "Assignment title is required.";
            return RedirectToPage();
        }

        var ok = await _db.CreateAssignmentAsync(classOfferingId, title, description, maxScore > 0 ? maxScore : 100m, dueDate);
        Message = ok ? "New assignment published and assigned to enrolled students." : "Failed to create assignment.";

        return RedirectToPage();
    }
}
