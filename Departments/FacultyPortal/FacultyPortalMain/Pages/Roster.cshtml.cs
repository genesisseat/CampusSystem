using FacultyPortalMain.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FacultyPortalMain.Pages;

public class RosterModel : PageModel
{
    private readonly FacultyDbService _db;

    public RosterModel(FacultyDbService db)
    {
        _db = db;
    }

    public List<FacultyDbService.ClassOfferingItem> Offerings { get; set; } = new();
    public List<FacultyDbService.StudentRosterItem> EnrolledStudents { get; set; } = new();
    public int SelectedOfferingId { get; set; }
    public FacultyDbService.ClassOfferingItem? CurrentOffering { get; set; }

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
        EnrolledStudents = await _db.GetClassRosterAsync(SelectedOfferingId);
    }
}
