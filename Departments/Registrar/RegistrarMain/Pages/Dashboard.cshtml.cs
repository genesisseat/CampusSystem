using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RegistrarMain.Models;
using RegistrarMain.Services;

namespace RegistrarMain.Pages;

public class DashboardModel : PageModel
{
    private readonly DatabaseService _db;
    private readonly SettingsService _settings;

    public DashboardModel(DatabaseService db, SettingsService settings)
    {
        _db = db;
        _settings = settings;
    }

    public string CurrentSchoolYear { get; set; } = "2025-2026";
    public string CurrentSemester { get; set; } = "1st Semester";
    public bool EncodingOpen { get; set; } = true;
    public string EncodingDeadline { get; set; } = "2025-10-30";

    public int TotalStudents { get; set; }
    public int ActiveEnrollments { get; set; }
    public int GraduatingStudents { get; set; }
    public int PendingDocs { get; set; }
    public int PendingAddDrop { get; set; }
    public int PendingOverload { get; set; }
    public int PendingRevisions { get; set; }
    public int TotalPendingQueue => PendingDocs + PendingAddDrop + PendingOverload + PendingRevisions;
    public int ActiveOfferingsCount { get; set; }

    public Dictionary<string, int> EnrollmentByYear { get; set; } = new();
    public List<ProgramStat> ProgramDistribution { get; set; } = new();
    public List<ActivityLog> RecentActivity { get; set; } = new();
    public List<SystemStatus> SystemStatuses { get; set; } = new();

    public class ProgramStat
    {
        public string Program { get; set; } = "";
        public int Cnt { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(string? switch_role)
    {
        if (switch_role == "registrar")
        {
            _db.SwitchToRegistrar();
            return RedirectToPage("/Dashboard");
        }

        _db.EnsureRegistrarSession();

        CurrentSchoolYear = await _settings.GetSchoolYearAsync();
        CurrentSemester = await _settings.GetSemesterAsync();
        EncodingOpen = await _settings.IsEncodingOpenAsync();
        EncodingDeadline = await _settings.GetEncodingDeadlineAsync();

        var conn = _db.Connection;

        TotalStudents = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM `user` WHERE role = 'student'");

        ActiveEnrollments = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM enrollments WHERE school_year = @sy AND semester = @sem AND status = 'active'",
            new { sy = CurrentSchoolYear, sem = CurrentSemester });

        GraduatingStudents = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM student_profile WHERE academic_status = 'Graduating'");

        PendingDocs = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM transcript_requests WHERE status IN ('pending', 'processing')");

        PendingAddDrop = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM add_drop_requests WHERE status = 'pending'");

        PendingOverload = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM overload_waiver_requests WHERE status = 'pending'");

        PendingRevisions = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM completion_revision_requests WHERE status = 'pending'");

        ActiveOfferingsCount = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM class_offerings WHERE status = 'open'");

        // Activity log
        var logs = await conn.QueryAsync<ActivityLog>(
            "SELECT message, created_at AS CreatedAt FROM activity_log ORDER BY created_at DESC LIMIT 6");
        RecentActivity = logs.AsList();

        // Program distribution
        var progRows = await conn.QueryAsync<ProgramStat>(
            "SELECT program, COUNT(*) as cnt FROM student_profile GROUP BY program ORDER BY cnt DESC");
        ProgramDistribution = progRows.AsList();

        // Distribution by year
        var yearRows = await conn.QueryAsync<(string YearLevel, int Cnt)>(
            "SELECT sp.year_level AS YearLevel, COUNT(*) AS Cnt FROM student_profile sp " +
            "JOIN `user` u ON u.id = sp.user_id AND u.role = 'student' GROUP BY sp.year_level");

        var yearMap = yearRows.ToDictionary(x => x.YearLevel, x => x.Cnt);
        var yearOrder = new[] { "1st Year", "2nd Year", "3rd Year", "4th Year" };
        foreach (var yr in yearOrder)
        {
            EnrollmentByYear[yr] = yearMap.TryGetValue(yr, out var c) ? c : 0;
        }

        // System status
        var statusRows = await conn.QueryAsync<SystemStatus>(
            "SELECT service_name AS ServiceName, status FROM system_status");
        SystemStatuses = statusRows.AsList();

        return Page();
    }

    public async Task<IActionResult> OnPostSaveTermSettingsAsync(string school_year, string semester, string? encoding_open, string? deadline)
    {
        _db.EnsureRegistrarSession();

        var sy = (school_year ?? "").Trim();
        var sem = (semester ?? "").Trim();
        var enc = encoding_open == "1" ? "1" : "0";
        var dead = (deadline ?? "").Trim();

        if (!string.IsNullOrEmpty(sy) && !string.IsNullOrEmpty(sem))
        {
            await _db.SetSettingAsync("current_school_year", sy);
            await _db.SetSettingAsync("current_semester", sem);
            await _db.SetSettingAsync("grade_encoding_open", enc);
            await _db.SetSettingAsync("grade_encoding_deadline", dead);

            await _db.LogActivityAsync(_db.CurrentUserId,
                $"Academic term updated to AY {sy} {sem}. Grade encoding window: {(enc == "1" ? "OPEN" : "CLOSED")}");

            TempData["FlashMessage"] = "Academic Term settings updated successfully.";
            TempData["FlashType"] = "success";
        }

        return RedirectToPage("/Dashboard");
    }
}
