using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FinanceMain.Pages;

public class AidModel : PageModel
{
    public const string AcademicYear = "2026-2027";
    public const string Semester = "1st Semester";

    public record ScholarshipProgram(string Code, string Name, string Coverage, int RecipientsCount, decimal TotalBudget);
    public record ScholarshipGrantee(string StudentName, string StudentNumber, string Program, string ScholarshipName, decimal DiscountAmount, string Status);

    public List<ScholarshipProgram> Programs { get; set; } = [];
    public static readonly List<ScholarshipGrantee> SharedGrantees =
    [
        new("Reyes, Maria Clara D.", "2023-00847", "BS Information Technology", "University Academic Honors Scholarship", 12000.00m, "Active"),
        new("Aguilar, Renzo Martin P.", "2023-00801", "BS Information Technology", "Varsity Athlete Athletic Scholarship", 32500.00m, "Active"),
        new("Bautista, Celine Joy A.", "2023-00815", "BS Information Technology", "Presidential Academic Excellence Grant", 32500.00m, "Active"),
        new("Cruz, Danielle Mae S.", "2023-00822", "BS Information Technology", "Sibling Enrollment Discount", 3250.00m, "Active"),
        new("Espinosa, Francine Nicole L.", "2023-00839", "BS Computer Science", "University Academic Honors Scholarship", 17000.00m, "Active"),
        new("Hernandez, Katrina Marie V.", "2023-00845", "BS Information Technology", "Presidential Academic Excellence Grant", 32500.00m, "Active"),
        new("Mendoza, Alyssa Bea R.", "2024-00101", "BS Information Technology", "Presidential Academic Excellence Grant", 32500.00m, "Active")
    ];

    public List<ScholarshipGrantee> Grantees { get; set; } = [];

    public decimal TotalScholarshipDisbursed => Grantees.Sum(g => g.DiscountAmount);
    public int ActiveScholarCount => Grantees.Count(g => g.Status == "Active");

    [TempData]
    public string? StatusMessage { get; set; }

    public void OnGet()
    {
        LoadData();
    }

    public IActionResult OnPostGrantScholarship(string studentName, string studentNumber, string program, string scholarshipName, decimal discountAmount)
    {
        if (!string.IsNullOrWhiteSpace(studentName) && discountAmount > 0)
        {
            SharedGrantees.Insert(0, new ScholarshipGrantee(
                studentName.Trim(),
                studentNumber.Trim(),
                program.Trim(),
                scholarshipName.Trim(),
                discountAmount,
                "Active"
            ));

            StatusMessage = $"Institutional scholarship successfully granted to {studentName} ({discountAmount:C}).";
        }

        return RedirectToPage();
    }

    private void LoadData()
    {
        Programs =
        [
            new("ACAD-100", "Presidential Academic Excellence Grant", "100% Tuition & Misc Discount", 14, 455000.00m),
            new("ACAD-50", "University Academic Honors Scholarship", "50% Tuition Discount", 28, 448000.00m),
            new("ATH-100", "Varsity Athlete Athletic Scholarship", "100% Tuition + Uniform Stipend", 18, 585000.00m),
            new("EMP-DEP", "Faculty & Staff Dependent Benefit", "75% Tuition Discount", 12, 288000.00m),
            new("SIBL-10", "Sibling Enrollment Discount", "10% Tuition Discount", 35, 112000.00m)
        ];

        Grantees = SharedGrantees.ToList();
    }
}
