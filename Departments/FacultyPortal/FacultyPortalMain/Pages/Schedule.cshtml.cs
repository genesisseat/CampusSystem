using FacultyPortalMain.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FacultyPortalMain.Pages;

public class ScheduleModel : PageModel
{
    private readonly FacultyDbService _db;

    public ScheduleModel(FacultyDbService db)
    {
        _db = db;
    }

    public List<FacultyDbService.ClassOfferingItem> Classes { get; set; } = new();

    public async Task OnGetAsync()
    {
        Classes = await _db.GetClassOfferingsAsync();
    }
}
