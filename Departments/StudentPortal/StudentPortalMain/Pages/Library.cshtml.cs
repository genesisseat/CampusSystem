using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StudentPortalMain.Models;
using StudentPortalMain.Services;

namespace StudentPortalMain.Pages;

public class LibraryModel : PageModel
{
    private readonly StudentPortalDbService _db;

    public LibraryModel(StudentPortalDbService db)
    {
        _db = db;
    }

    public StudentUser? Student { get; set; }
    public StudentProfileData Profile { get; set; } = new();
    public List<LibraryReservationRecord> Reservations { get; set; } = new();

    public async Task OnGetAsync()
    {
        var studentId = _db.CurrentStudentId;
        Student = await _db.GetStudentUserAsync(studentId);
        Profile = await _db.GetStudentProfileAsync(studentId);

        Reservations = await _db.GetLibraryReservationsAsync(studentId);
    }

    public async Task<IActionResult> OnPostReserveAsync(string bookTitle, string bookAuthor)
    {
        var studentId = _db.CurrentStudentId;
        if (string.IsNullOrWhiteSpace(bookTitle))
        {
            TempData["FlashMessage"] = "Please provide the title of the book to reserve.";
            TempData["FlashType"] = "danger";
            return RedirectToPage();
        }

        await _db.SubmitLibraryReservationAsync(studentId, bookTitle.Trim(), bookAuthor?.Trim() ?? "University Academic Press");

        TempData["FlashMessage"] = $"Book reservation for '{bookTitle}' placed successfully! Pick up within 48 hours at University Library Circulation.";
        TempData["FlashType"] = "success";
        return RedirectToPage();
    }
}
