using FacultyPortalMain.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FacultyPortalMain.Pages;

public class AttendanceModel : PageModel
{
    private readonly FacultyDbService _db;

    public AttendanceModel(FacultyDbService db)
    {
        _db = db;
    }

    public List<FacultyDbService.ClassOfferingItem> Offerings { get; set; } = new();
    public int SelectedOfferingId { get; set; }
    public DateTime SessionDate { get; set; } = DateTime.Today;
    public string Topic { get; set; } = "";
    public List<AttendanceRecordDto> Students { get; set; } = new();

    [TempData]
    public string? Message { get; set; }

    public async Task OnGetAsync(int? offeringId, DateTime? date)
    {
        Offerings = await _db.GetClassOfferingsAsync();
        SelectedOfferingId = offeringId ?? Offerings.FirstOrDefault()?.Id ?? 0;
        SessionDate = date ?? DateTime.Today;

        if (SelectedOfferingId > 0)
        {
            Students = await _db.GetAttendanceSheetAsync(SelectedOfferingId, SessionDate);
        }
    }

    public async Task<IActionResult> OnPostAsync(int offeringId, DateTime sessionDate, string? topic, List<int> studentIds, List<string> statuses)
    {
        var items = new List<(int StudentId, string Status)>();
        for (int i = 0; i < studentIds.Count && i < statuses.Count; i++)
        {
            items.Add((studentIds[i], statuses[i]));
        }

        var ok = await _db.SaveAttendanceBatchAsync(offeringId, sessionDate, topic, items);
        Message = ok ? "Attendance recorded successfully to MySQL database." : "Failed to record attendance.";

        return RedirectToPage(new { offeringId, date = sessionDate.ToString("yyyy-MM-dd") });
    }
}
