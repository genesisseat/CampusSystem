using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StudentPortalMain.Models;
using StudentPortalMain.Services;

namespace StudentPortalMain.Pages;

public class ProfileModel : PageModel
{
    private readonly StudentPortalDbService _db;

    public ProfileModel(StudentPortalDbService db)
    {
        _db = db;
    }

    public StudentUser? Student { get; set; }
    public StudentProfileData Profile { get; set; } = new();
    public string? NstpSerial { get; set; }
    public string? NstpType { get; set; }
    public string? ChedSoNumber { get; set; }

    [BindProperty]
    public string ContactNumber { get; set; } = "";

    [BindProperty]
    public string Address { get; set; } = "";

    public async Task OnGetAsync()
    {
        var studentId = _db.CurrentStudentId;
        Student = await _db.GetStudentUserAsync(studentId);
        Profile = await _db.GetStudentProfileAsync(studentId);

        ContactNumber = Profile.ContactNumber ?? "";
        Address = Profile.Address ?? "";

        var conn = _db.Connection;

        // Check NSTP
        var nstp = await conn.QueryFirstOrDefaultAsync<(string SerialNumber, string Component)>(
            "SELECT serial_number AS SerialNumber, nstp_component AS Component FROM nstp_serial_numbers WHERE student_id = @studentId LIMIT 1",
            new { studentId });
        if (!string.IsNullOrEmpty(nstp.SerialNumber))
        {
            NstpSerial = nstp.SerialNumber;
            NstpType = nstp.Component;
        }

        // Check CHED SO
        var ched = await conn.QueryFirstOrDefaultAsync<string>(
            "SELECT so_number FROM ched_special_orders WHERE student_id = @studentId LIMIT 1",
            new { studentId });
        ChedSoNumber = ched;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var studentId = _db.CurrentStudentId;
        await _db.UpdateContactInfoAsync(studentId, ContactNumber?.Trim() ?? "", Address?.Trim() ?? "");

        TempData["FlashMessage"] = "Your contact information and address have been updated successfully.";
        TempData["FlashType"] = "success";
        return RedirectToPage();
    }
}
