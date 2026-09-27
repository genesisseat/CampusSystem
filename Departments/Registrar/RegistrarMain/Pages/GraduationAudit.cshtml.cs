using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RegistrarMain.Models;
using RegistrarMain.Services;

namespace RegistrarMain.Pages;

public class GraduationAuditModel : PageModel
{
    private readonly DatabaseService _db;
    private readonly SettingsService _settings;

    public GraduationAuditModel(DatabaseService db, SettingsService settings)
    {
        _db = db;
        _settings = settings;
    }

    public string CurrentSchoolYear { get; set; } = "2025-2026";
    public string ActiveTab { get; set; } = "candidates";

    public List<CandidateAuditItem> CandidateAudit { get; set; } = new();
    public List<CandidateAuditItem> HonorsList { get; set; } = new();

    public class CandidateAuditItem
    {
        public int Id { get; set; }
        public string StudentIdNumber { get; set; } = "";
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Program { get; set; } = "";
        public string YearLevel { get; set; } = "";
        public decimal? AverageGrade { get; set; }
        public int CompletedUnits { get; set; }
        public int UnitsRemaining { get; set; }
        public string AcademicStatus { get; set; } = "";
        public string GuidanceClearanceStatus { get; set; } = "";
        public int UnverifiedDocsCount { get; set; }
        public int HasNstp { get; set; }
        public int AcademicDeficienciesCount { get; set; }

        public bool IsEligible { get; set; }
        public List<string> Deficiencies { get; set; } = new();
        public string? HonorTitle { get; set; }
        public string HonorBadge { get; set; } = "badge-on-leave";
    }

    public async Task<IActionResult> OnGetAsync(string tab = "candidates")
    {
        _db.EnsureRegistrarSession();
        ActiveTab = new[] { "candidates", "honors", "print_roll" }.Contains(tab) ? tab : "candidates";

        CurrentSchoolYear = await _settings.GetSchoolYearAsync();

        var conn = _db.Connection;

        var rawList = await conn.QueryAsync<CandidateAuditItem>(
            "SELECT u.id, u.student_id_number AS StudentIdNumber, u.name, u.email, sp.program, sp.year_level AS YearLevel, " +
            "sp.average_grade AS AverageGrade, sp.completed_units AS CompletedUnits, sp.units_remaining AS UnitsRemaining, " +
            "sp.academic_status AS AcademicStatus, sp.guidance_clearance_status AS GuidanceClearanceStatus, " +
            "(SELECT COUNT(*) FROM document_credentials dc WHERE dc.student_id = u.id AND dc.status != 'Verified') AS UnverifiedDocsCount, " +
            "(SELECT COUNT(*) FROM nstp_serial_numbers nsn WHERE nsn.student_id = u.id) AS HasNstp, " +
            "(SELECT COUNT(*) FROM enrolled_subjects es JOIN grades g ON g.enrolled_subject_id = es.id JOIN enrollments e ON e.id = es.enrollment_id WHERE e.student_id = u.id AND (g.is_inc = 1 OR g.grade > 3.00)) AS AcademicDeficienciesCount " +
            "FROM `user` u " +
            "JOIN student_profile sp ON sp.user_id = u.id " +
            "WHERE sp.year_level = '4th Year' OR sp.academic_status IN ('Graduating', 'Graduated') " +
            "ORDER BY sp.average_grade ASC, u.name ASC");

        foreach (var c in rawList)
        {
            var gwa = (double)(c.AverageGrade ?? 5.00m);
            var deficiencies = new List<string>();

            if (c.CompletedUnits < 138)
            {
                deficiencies.Add($"Lacks {144 - c.CompletedUnits} curriculum units");
            }
            if (c.AcademicDeficienciesCount > 0)
            {
                deficiencies.Add("Has pending INC or failing mark");
            }
            if (c.UnverifiedDocsCount > 0)
            {
                deficiencies.Add("201 credentials incomplete");
            }
            if (c.HasNstp == 0)
            {
                deficiencies.Add("Missing NSTP Serial Number");
            }
            if (c.GuidanceClearanceStatus != "Cleared")
            {
                deficiencies.Add("Guidance clearance hold");
            }

            var isEligible = deficiencies.Count == 0;
            string? honorTitle = null;
            var honorBadge = "badge-on-leave";

            if (gwa >= 1.00 && gwa <= 1.20 && c.AcademicDeficienciesCount == 0)
            {
                honorTitle = "Summa Cum Laude";
                honorBadge = "badge-active";
            }
            else if (gwa > 1.20 && gwa <= 1.45 && c.AcademicDeficienciesCount == 0)
            {
                honorTitle = "Magna Cum Laude";
                honorBadge = "badge-open";
            }
            else if (gwa > 1.45 && gwa <= 1.75 && c.AcademicDeficienciesCount == 0)
            {
                honorTitle = "Cum Laude";
                honorBadge = "badge-open";
            }

            c.IsEligible = isEligible;
            c.Deficiencies = deficiencies;
            c.HonorTitle = honorTitle;
            c.HonorBadge = honorBadge;

            CandidateAudit.Add(c);

            if (honorTitle != null && isEligible)
            {
                HonorsList.Add(c);
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCertifyGraduateAsync(int student_id)
    {
        _db.EnsureRegistrarSession();
        if (student_id > 0)
        {
            var conn = _db.Connection;
            await conn.ExecuteAsync(
                "UPDATE student_profile SET academic_status = 'Graduated', enrollment_status = 'Graduated' WHERE user_id = @sid",
                new { sid = student_id });

            await _db.LogActivityAsync(student_id, $"Student officially certified as Graduated by {_db.CurrentUserName}.");
            TempData["FlashMessage"] = "Candidate successfully certified as official graduate.";
            TempData["FlashType"] = "success";
        }
        return RedirectToPage("/GraduationAudit", new { tab = "candidates" });
    }
}
