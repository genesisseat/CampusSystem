using System.Text;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RegistrarMain.Models;
using RegistrarMain.Services;

namespace RegistrarMain.Pages;

public class GovernmentComplianceModel : PageModel
{
    private readonly DatabaseService _db;
    private readonly SettingsService _settings;

    public GovernmentComplianceModel(DatabaseService db, SettingsService settings)
    {
        _db = db;
        _settings = settings;
    }

    public string CurrentSchoolYear { get; set; } = "2025-2026";
    public string CurrentSemester { get; set; } = "1st Semester";
    public string SchoolName { get; set; } = "Metropolitan State University";
    public string SchoolCode { get; set; } = "MSU-0422";
    public string ActiveTab { get; set; } = "so";

    public List<StudentDropdownItem> GraduatingStudents { get; set; } = new();
    public List<ChedSoItem> SoRecords { get; set; } = new();
    public List<NstpSerialItem> NstpRecords { get; set; } = new();
    public List<MasterlistItem> EnrolledMasterlist { get; set; } = new();

    public class StudentDropdownItem
    {
        public int Id { get; set; }
        public string StudentIdNumber { get; set; } = "";
        public string Name { get; set; } = "";
        public string Program { get; set; } = "";
    }

    public class ChedSoItem
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = "";
        public string StudentIdNumber { get; set; } = "";
        public string Email { get; set; } = "";
        public string SoNumber { get; set; } = "";
        public string SeriesYear { get; set; } = "";
        public string Program { get; set; } = "";
        public DateTime DateApplied { get; set; }
        public DateTime? DateIssued { get; set; }
        public string Status { get; set; } = "Applied";
        public string? Remarks { get; set; }
    }

    public class NstpSerialItem
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = "";
        public string StudentIdNumber { get; set; } = "";
        public string Program { get; set; } = "";
        public string NstpComponent { get; set; } = "CWTS";
        public string SerialNumber { get; set; } = "";
        public DateTime DateIssued { get; set; }
        public string? Remarks { get; set; }
    }

    public class MasterlistItem
    {
        public int Id { get; set; }
        public string StudentIdNumber { get; set; } = "";
        public string Name { get; set; } = "";
        public string Gender { get; set; } = "";
        public string Program { get; set; } = "";
        public string YearLevel { get; set; } = "";
        public int TotalUnits { get; set; }
        public string EnrollmentType { get; set; } = "";
        public string EnrollmentStatus { get; set; } = "active";
    }

    public async Task<IActionResult> OnGetAsync(string tab = "so", string? export = null)
    {
        _db.EnsureRegistrarSession();
        ActiveTab = new[] { "so", "nstp", "masterlist" }.Contains(tab) ? tab : "so";

        CurrentSchoolYear = await _settings.GetSchoolYearAsync();
        CurrentSemester = await _settings.GetSemesterAsync();
        SchoolName = await _db.GetSettingAsync("school_name", "National University Lipa");
        SchoolCode = await _db.GetSettingAsync("school_code", "NU-LIPA-04");

        var conn = _db.Connection;

        if (export == "csv")
        {
            var exportRows = await conn.QueryAsync<MasterlistItem>(
                "SELECT u.student_id_number AS StudentIdNumber, u.name, sp.gender, sp.program, sp.year_level AS YearLevel, " +
                "e.total_units AS TotalUnits, e.enrollment_type AS EnrollmentType, e.status AS EnrollmentStatus " +
                "FROM enrollments e " +
                "JOIN `user` u ON u.id = e.student_id " +
                "LEFT JOIN student_profile sp ON sp.user_id = u.id " +
                "WHERE e.school_year = @sy AND e.semester = @sem AND e.status = 'active' " +
                "ORDER BY sp.program ASC, sp.year_level ASC, u.name ASC",
                new { sy = CurrentSchoolYear, sem = CurrentSemester });

            var sb = new StringBuilder();
            sb.AppendLine($"\"CHED Institutional Code\",\"{SchoolCode}\"");
            sb.AppendLine($"\"Higher Education Institution\",\"{SchoolName}\"");
            sb.AppendLine($"\"Academic School Year\",\"{CurrentSchoolYear}\"");
            sb.AppendLine($"\"Academic Semester\",\"{CurrentSemester}\"");
            sb.AppendLine();
            sb.AppendLine("\"Student ID Number\",\"Full Legal Name\",\"Sex\",\"Degree Program\",\"Year Level\",\"Total Units Enrolled\",\"Matriculation Type\",\"Enrollment Status\"");

            foreach (var er in exportRows)
            {
                sb.AppendLine($"\"{er.StudentIdNumber}\",\"{er.Name}\",\"{er.Gender}\",\"{er.Program}\",\"{er.YearLevel}\",{er.TotalUnits},\"{er.EnrollmentType.ToUpper()}\",\"{er.EnrollmentStatus.ToUpper()}\"");
            }

            var fileName = $"CHED_Masterlist_{CurrentSchoolYear.Replace(" ", "_")}_{CurrentSemester.Replace(" ", "_")}.csv";
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", fileName);
        }

        var grads = await conn.QueryAsync<StudentDropdownItem>(
            "SELECT u.id, u.student_id_number AS StudentIdNumber, u.name, sp.program " +
            "FROM `user` u JOIN student_profile sp ON sp.user_id = u.id WHERE u.role = 'student' ORDER BY u.name ASC");
        GraduatingStudents = grads.AsList();

        var so = await conn.QueryAsync<ChedSoItem>(
            "SELECT cso.id, cso.student_id AS StudentId, cso.so_number AS SoNumber, cso.series_year AS SeriesYear, cso.program, " +
            "cso.date_applied AS DateApplied, cso.date_issued AS DateIssued, cso.status, cso.remarks, " +
            "u.name AS StudentName, u.student_id_number AS StudentIdNumber, u.email " +
            "FROM ched_special_orders cso " +
            "JOIN `user` u ON u.id = cso.student_id " +
            "ORDER BY cso.date_applied DESC");
        SoRecords = so.AsList();

        var nstp = await conn.QueryAsync<NstpSerialItem>(
            "SELECT nsn.id, nsn.student_id AS StudentId, nsn.nstp_component AS NstpComponent, nsn.serial_number AS SerialNumber, " +
            "nsn.date_issued AS DateIssued, nsn.remarks, u.name AS StudentName, u.student_id_number AS StudentIdNumber, sp.program " +
            "FROM nstp_serial_numbers nsn " +
            "JOIN `user` u ON u.id = nsn.student_id " +
            "LEFT JOIN student_profile sp ON sp.user_id = u.id " +
            "ORDER BY nsn.date_issued DESC");
        NstpRecords = nstp.AsList();

        var mlist = await conn.QueryAsync<MasterlistItem>(
            "SELECT e.id, e.total_units AS TotalUnits, e.enrollment_type AS EnrollmentType, e.status AS EnrollmentStatus, " +
            "u.student_id_number AS StudentIdNumber, u.name, sp.gender, sp.program, sp.year_level AS YearLevel " +
            "FROM enrollments e " +
            "JOIN `user` u ON u.id = e.student_id " +
            "LEFT JOIN student_profile sp ON sp.user_id = u.id " +
            "WHERE e.school_year = @sy AND e.semester = @sem AND e.status = 'active' " +
            "ORDER BY sp.program ASC, u.name ASC",
            new { sy = CurrentSchoolYear, sem = CurrentSemester });
        EnrolledMasterlist = mlist.AsList();

        return Page();
    }

    public async Task<IActionResult> OnPostRecordSoAsync(
        int student_id, string so_number, string series_year, string program, string status, string? date_applied, string? date_issued, string? remarks)
    {
        _db.EnsureRegistrarSession();
        var soNum = (so_number ?? "").Trim();
        var yr = (series_year ?? DateTime.Now.Year.ToString()).Trim();

        if (student_id > 0 && !string.IsNullOrEmpty(soNum))
        {
            var dateApp = !string.IsNullOrEmpty(date_applied) ? date_applied : DateTime.Today.ToString("yyyy-MM-dd");
            var conn = _db.Connection;

            await conn.ExecuteAsync(
                "INSERT INTO ched_special_orders (student_id, so_number, series_year, program, date_applied, date_issued, status, remarks) " +
                "VALUES (@student_id, @soNum, @yr, @program, @dateApp, @dateIss, @status, @remarks)",
                new { student_id, soNum, yr, program = (program ?? "BS Information Technology").Trim(), dateApp, dateIss = string.IsNullOrEmpty(date_issued) ? null : date_issued, status = (status ?? "Applied").Trim(), remarks = (remarks ?? "").Trim() });

            await _db.LogActivityAsync(student_id, $"CHED Special Order recorded ({soNum}) by {_db.CurrentUserName}.");
            TempData["FlashMessage"] = "CHED Special Order recorded successfully.";
            TempData["FlashType"] = "success";
        }
        else
        {
            TempData["FlashMessage"] = "Please provide student and S.O. number.";
            TempData["FlashType"] = "error";
        }

        return RedirectToPage("/GovernmentCompliance", new { tab = "so" });
    }

    public async Task<IActionResult> OnPostRecordNstpAsync(
        int student_id, string nstp_component, string serial_number, string? date_issued, string? remarks)
    {
        _db.EnsureRegistrarSession();
        var comp = (nstp_component ?? "CWTS").Trim();
        var sNum = (serial_number ?? "").Trim().ToUpper();

        if (student_id > 0 && !string.IsNullOrEmpty(sNum))
        {
            var dateIss = !string.IsNullOrEmpty(date_issued) ? date_issued : DateTime.Today.ToString("yyyy-MM-dd");
            var conn = _db.Connection;

            await conn.ExecuteAsync(
                "INSERT INTO nstp_serial_numbers (student_id, nstp_component, serial_number, date_issued, remarks) " +
                "VALUES (@student_id, @comp, @sNum, @dateIss, @remarks)",
                new { student_id, comp, sNum, dateIss, remarks = (remarks ?? "").Trim() });

            await _db.LogActivityAsync(student_id, $"NSTP Serial Number recorded ({sNum}) by {_db.CurrentUserName}.");
            TempData["FlashMessage"] = "NSTP Serial Number recorded successfully.";
            TempData["FlashType"] = "success";
        }
        else
        {
            TempData["FlashMessage"] = "Please provide student and serial number.";
            TempData["FlashType"] = "error";
        }

        return RedirectToPage("/GovernmentCompliance", new { tab = "nstp" });
    }
}
