using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StudentPortalMain.Services;

namespace StudentPortalMain.Pages;

public class SwitchStudentModel : PageModel
{
    private readonly StudentPortalDbService _db;

    public SwitchStudentModel(StudentPortalDbService db)
    {
        _db = db;
    }

    public IActionResult OnGet(int id, string returnUrl = "/Dashboard")
    {
        if (id > 0)
        {
            _db.SwitchStudent(id);
            TempData["FlashMessage"] = "Switched active student session successfully.";
            TempData["FlashType"] = "success";
        }

        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToPage("/Dashboard");
    }
}
