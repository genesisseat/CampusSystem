using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StudentPortalMain.Models;
using StudentPortalMain.Services;

namespace StudentPortalMain.Pages;

public class RequestsModel : PageModel
{
    private readonly StudentPortalDbService _db;

    public RequestsModel(StudentPortalDbService db)
    {
        _db = db;
    }

    public StudentUser? Student { get; set; }
    public StudentProfileData Profile { get; set; } = new();

    public record GuidanceTicket(string Title, string Category, string Details, string Urgency, string Status, DateTime CreatedAt);
    public static readonly List<GuidanceTicket> MockTickets = new();

    public List<GuidanceTicket> MyTickets => MockTickets;

    public async Task OnGetAsync()
    {
        var studentId = _db.CurrentStudentId;
        Student = await _db.GetStudentUserAsync(studentId);
        Profile = await _db.GetStudentProfileAsync(studentId);

        if (!MockTickets.Any())
        {
            MockTickets.Add(new GuidanceTicket("Academic Consultation - Shifting Advice", "Academic", "Seeking advice regarding shifting curriculum load", "Normal", "Completed", DateTime.UtcNow.AddDays(-14)));
        }
    }

    public async Task<IActionResult> OnPostAsync(string subject, string category, string urgency, string details)
    {
        var studentId = _db.CurrentStudentId;
        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(details))
        {
            TempData["FlashMessage"] = "Please fill out all required fields.";
            TempData["FlashType"] = "danger";
            return RedirectToPage();
        }

        MockTickets.Insert(0, new GuidanceTicket(subject.Trim(), category ?? "Academic", details.Trim(), urgency ?? "Normal", "Open", DateTime.UtcNow));
        await _db.LogActivityAsync(studentId, $"Submitted Guidance ticket ({category}: {subject}) via Student Portal");

        TempData["FlashMessage"] = "Your guidance request has been submitted. A counselor will reach out via your institutional email.";
        TempData["FlashType"] = "success";
        return RedirectToPage();
    }
}
