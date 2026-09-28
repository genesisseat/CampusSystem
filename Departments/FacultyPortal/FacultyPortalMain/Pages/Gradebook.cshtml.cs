using FacultyPortalMain.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FacultyPortalMain.Pages;

public class GradebookModel : PageModel
{
    private readonly FacultyDbService _db;

    public GradebookModel(FacultyDbService db)
    {
        _db = db;
    }

    public List<FacultyDbService.ClassOfferingItem> Offerings { get; set; } = new();
    public List<FacultyDbService.StudentRosterItem> Students { get; set; } = new();
    public int SelectedOfferingId { get; set; }
    public FacultyDbService.ClassOfferingItem? CurrentOffering { get; set; }

    [TempData]
    public string? FlashMessage { get; set; }

    public async Task OnGetAsync(int? sectionId = null)
    {
        Offerings = await _db.GetClassOfferingsAsync();

        if (sectionId.HasValue && sectionId.Value > 0)
        {
            SelectedOfferingId = sectionId.Value;
        }
        else if (Offerings.Any())
        {
            SelectedOfferingId = Offerings.First().Id;
        }

        CurrentOffering = Offerings.FirstOrDefault(o => o.Id == SelectedOfferingId);
        Students = await _db.GetClassRosterAsync(SelectedOfferingId);
    }

    public async Task<IActionResult> OnPostSaveGradeAsync(int sectionId, int enrolledSubjectId, decimal grade, string? remarks)
    {
        if (enrolledSubjectId > 0 && grade >= 1.00m && grade <= 5.00m)
        {
            await _db.SaveGradeAsync(enrolledSubjectId, grade, remarks ?? "Submitted by Faculty");
            FlashMessage = $"Grade of {grade:F2} successfully saved to University Database!";
        }
        return RedirectToPage(new { sectionId });
    }
}
