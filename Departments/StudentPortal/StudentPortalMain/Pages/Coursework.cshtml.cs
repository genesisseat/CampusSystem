using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StudentPortalMain.Models;
using StudentPortalMain.Services;

namespace StudentPortalMain.Pages;

public class CourseworkModel : PageModel
{
    private readonly StudentPortalDbService _db;

    public CourseworkModel(StudentPortalDbService db)
    {
        _db = db;
    }

    public StudentUser? Student { get; set; }
    public StudentProfileData Profile { get; set; } = new();
    public List<AssignmentItem> Assignments { get; set; } = new();

    public async Task OnGetAsync()
    {
        var studentId = _db.CurrentStudentId;
        Student = await _db.GetStudentUserAsync(studentId);
        Profile = await _db.GetStudentProfileAsync(studentId);

        Assignments = await _db.GetAssignmentsAsync(studentId);
    }

    public async Task<IActionResult> OnPostSubmitWorkAsync(int assignmentId)
    {
        var studentId = _db.CurrentStudentId;
        if (assignmentId > 0)
        {
            await _db.SubmitAssignmentWorkAsync(assignmentId, studentId);
            TempData["FlashMessage"] = "Coursework assignment submitted successfully to the faculty instructor!";
            TempData["FlashType"] = "success";
        }
        return RedirectToPage();
    }
}
