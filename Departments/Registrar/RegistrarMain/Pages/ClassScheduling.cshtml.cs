using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RegistrarMain.Models;
using RegistrarMain.Services;

namespace RegistrarMain.Pages;

public class ClassSchedulingModel : PageModel
{
    private readonly DatabaseService _db;
    private readonly SettingsService _settings;

    public ClassSchedulingModel(DatabaseService db, SettingsService settings)
    {
        _db = db;
        _settings = settings;
    }

    public static readonly string[] AllDays = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];

    public string CurrentSchoolYear { get; set; } = "2025-2026";
    public string CurrentSemester { get; set; } = "1st Semester";
    public string ActiveTab { get; set; } = "offerings";

    public List<SubjectItem> SubjectsList { get; set; } = new();
    public List<ClassOfferingItem> OfferingsList { get; set; } = new();

    public int SelectedOfferingId { get; set; }
    public ClassOfferingItem? SelectedOfferingDetails { get; set; }
    public List<RosterStudentItem> RosterStudents { get; set; } = new();

    public class SubjectItem
    {
        public int Id { get; set; }
        public string SubjectCode { get; set; } = "";
        public string SubjectName { get; set; } = "";
        public decimal Units { get; set; }
        public int LecHours { get; set; }
        public int LabHours { get; set; }
        public string? PrereqCode { get; set; }
        public string CurriculumProgram { get; set; } = "";
        public string YearLevel { get; set; } = "";
        public string Semester { get; set; } = "";
    }

    public class ClassOfferingItem
    {
        public int Id { get; set; }
        public int SubjectId { get; set; }
        public string SubjectCode { get; set; } = "";
        public string SubjectName { get; set; } = "";
        public decimal Units { get; set; }
        public string SectionCode { get; set; } = "";
        public string Room { get; set; } = "";
        public string DaysOfWeek { get; set; } = "";
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string InstructorName { get; set; } = "";
        public int Capacity { get; set; }
        public int SlotsTaken { get; set; }
        public string Status { get; set; } = "open";
    }

    public class RosterStudentItem
    {
        public int EnrolledSubId { get; set; }
        public string SubjectStatus { get; set; } = "";
        public int StudentId { get; set; }
        public string Name { get; set; } = "";
        public string StudentIdNumber { get; set; } = "";
        public string Email { get; set; } = "";
        public string Program { get; set; } = "";
        public string YearLevel { get; set; } = "";
        public decimal? Grade { get; set; }
        public bool IsInc { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(string tab = "offerings", int offering_id = 0)
    {
        _db.EnsureRegistrarSession();
        ActiveTab = new[] { "offerings", "subjects", "roster" }.Contains(tab) ? tab : "offerings";

        CurrentSchoolYear = await _settings.GetSchoolYearAsync();
        CurrentSemester = await _settings.GetSemesterAsync();

        var conn = _db.Connection;

        var subjects = await conn.QueryAsync<SubjectItem>(
            "SELECT s.id, s.subject_code AS SubjectCode, s.subject_name AS SubjectName, s.units, s.lec_hours AS LecHours, s.lab_hours AS LabHours, " +
            "s.curriculum_program AS CurriculumProgram, s.year_level AS YearLevel, s.semester, p.subject_code AS PrereqCode " +
            "FROM subjects s " +
            "LEFT JOIN subjects p ON p.id = s.prerequisite_subject_id " +
            "ORDER BY s.subject_code ASC");
        SubjectsList = subjects.AsList();

        var offerings = await conn.QueryAsync<ClassOfferingItem>(
            "SELECT co.id, co.subject_id AS SubjectId, s.subject_code AS SubjectCode, s.subject_name AS SubjectName, s.units, " +
            "co.section_code AS SectionCode, co.room, co.days_of_week AS DaysOfWeek, co.start_time AS StartTime, co.end_time AS EndTime, " +
            "co.instructor_name AS InstructorName, co.capacity, co.slots_taken AS SlotsTaken, co.status " +
            "FROM class_offerings co " +
            "JOIN subjects s ON s.id = co.subject_id " +
            "ORDER BY co.section_code ASC, s.subject_code ASC");
        OfferingsList = offerings.AsList();

        SelectedOfferingId = offering_id > 0 ? offering_id : (OfferingsList.FirstOrDefault()?.Id ?? 0);

        if (SelectedOfferingId > 0)
        {
            SelectedOfferingDetails = await conn.QueryFirstOrDefaultAsync<ClassOfferingItem>(
                "SELECT co.id, co.subject_id AS SubjectId, s.subject_code AS SubjectCode, s.subject_name AS SubjectName, s.units, " +
                "co.section_code AS SectionCode, co.room, co.days_of_week AS DaysOfWeek, co.start_time AS StartTime, co.end_time AS EndTime, " +
                "co.instructor_name AS InstructorName, co.capacity, co.slots_taken AS SlotsTaken, co.status " +
                "FROM class_offerings co " +
                "JOIN subjects s ON s.id = co.subject_id " +
                "WHERE co.id = @id",
                new { id = SelectedOfferingId });

            var roster = await conn.QueryAsync<RosterStudentItem>(
                "SELECT es.id AS EnrolledSubId, es.status AS SubjectStatus, u.id AS StudentId, u.name, u.student_id_number AS StudentIdNumber, u.email, " +
                "sp.program, sp.year_level AS YearLevel, g.grade, g.is_inc AS IsInc " +
                "FROM enrolled_subjects es " +
                "JOIN enrollments e ON e.id = es.enrollment_id " +
                "JOIN `user` u ON u.id = e.student_id " +
                "LEFT JOIN student_profile sp ON sp.user_id = u.id " +
                "LEFT JOIN grades g ON g.enrolled_subject_id = es.id " +
                "WHERE es.class_offering_id = @id AND es.status = 'enrolled' " +
                "ORDER BY u.name ASC",
                new { id = SelectedOfferingId });
            RosterStudents = roster.AsList();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAddSubjectAsync(
        string subject_code, string subject_name, decimal units, int lec_hours, int lab_hours, int? prerequisite_subject_id,
        string curriculum_program, string year_level, string semester)
    {
        _db.EnsureRegistrarSession();
        var code = (subject_code ?? "").Trim().ToUpper();
        var name = (subject_name ?? "").Trim();

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name))
        {
            TempData["FlashMessage"] = "Subject code and descriptive title are required.";
            TempData["FlashType"] = "error";
        }
        else
        {
            var conn = _db.Connection;
            await conn.ExecuteAsync(
                "INSERT INTO subjects (subject_code, subject_name, units, lec_hours, lab_hours, prerequisite_subject_id, curriculum_program, year_level, semester) " +
                "VALUES (@code, @name, @units, @lec, @lab, @prereq, @prog, @yr, @sem)",
                new { code, name, units, lec = lec_hours, lab = lab_hours, prereq = prerequisite_subject_id, prog = curriculum_program, yr = year_level, sem = semester });

            await _db.LogActivityAsync(_db.CurrentUserId, $"Subject {code} ({name}) added to catalog by {_db.CurrentUserName}.");
            TempData["FlashMessage"] = $"Subject {code} added successfully.";
            TempData["FlashType"] = "success";
        }

        return RedirectToPage("/ClassScheduling", new { tab = "subjects" });
    }

    public async Task<IActionResult> OnPostDeleteSubjectAsync(int subject_id)
    {
        _db.EnsureRegistrarSession();
        var conn = _db.Connection;
        var inUse = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM class_offerings WHERE subject_id = @id", new { id = subject_id });
        if (inUse > 0)
        {
            TempData["FlashMessage"] = "Cannot delete subject: class offerings currently reference this course.";
            TempData["FlashType"] = "error";
        }
        else
        {
            await conn.ExecuteAsync("DELETE FROM subjects WHERE id = @id", new { id = subject_id });
            TempData["FlashMessage"] = "Subject deleted from catalog.";
            TempData["FlashType"] = "success";
        }
        return RedirectToPage("/ClassScheduling", new { tab = "subjects" });
    }

    public async Task<IActionResult> OnPostAddOfferingAsync(
        int subject_id, string section_code, string room, int capacity, string instructor_name, string[] days, string start_time, string end_time)
    {
        _db.EnsureRegistrarSession();
        CurrentSchoolYear = await _settings.GetSchoolYearAsync();
        CurrentSemester = await _settings.GetSemesterAsync();

        var sec = (section_code ?? "").Trim().ToUpper();
        var rm = (room ?? "").Trim().ToUpper();
        var validDays = days?.Where(d => AllDays.Contains(d)).ToArray() ?? Array.Empty<string>();

        if (subject_id <= 0 || string.IsNullOrEmpty(sec) || string.IsNullOrEmpty(rm) || !validDays.Any() || string.IsNullOrEmpty(start_time) || string.IsNullOrEmpty(end_time))
        {
            TempData["FlashMessage"] = "Please complete all schedule details (subject, section, room, days, and time).";
            TempData["FlashType"] = "error";
        }
        else if (string.Compare(start_time, end_time, StringComparison.Ordinal) >= 0)
        {
            TempData["FlashMessage"] = "Start time must precede end time.";
            TempData["FlashType"] = "error";
        }
        else
        {
            var conn = _db.Connection;
            var candidates = await conn.QueryAsync<(string SectionCode, string DaysOfWeek, TimeSpan StartTime, TimeSpan EndTime, string SubjectCode, string Room)>(
                "SELECT co.section_code AS SectionCode, co.days_of_week AS DaysOfWeek, co.start_time AS StartTime, co.end_time AS EndTime, s.subject_code AS SubjectCode, co.room " +
                "FROM class_offerings co " +
                "JOIN subjects s ON s.id = co.subject_id " +
                "WHERE co.room = @rm AND co.school_year = @sy AND co.semester = @sem AND co.status != 'cancelled' " +
                "  AND co.start_time < @end AND co.end_time > @start",
                new { rm, sy = CurrentSchoolYear, sem = CurrentSemester, start = start_time, end = end_time });

            var conflict = candidates.FirstOrDefault(c =>
                c.DaysOfWeek.Split(',').Select(x => x.Trim()).Intersect(validDays).Any());

            if (conflict.SectionCode != null)
            {
                TempData["FlashMessage"] = $"Schedule Conflict: Room {rm} is already reserved by {conflict.SubjectCode} ({conflict.SectionCode}) on {conflict.DaysOfWeek} {conflict.StartTime}–{conflict.EndTime}.";
                TempData["FlashType"] = "error";
            }
            else
            {
                var daysStr = string.Join(",", validDays);
                await conn.ExecuteAsync(
                    "INSERT INTO class_offerings " +
                    "(subject_id, section_code, school_year, semester, room, days_of_week, start_time, end_time, instructor_name, capacity, slots_taken, status) " +
                    "VALUES (@subject_id, @sec, @sy, @sem, @rm, @daysStr, @start_time, @end_time, @instructor_name, @capacity, 0, 'open')",
                    new { subject_id, sec, sy = CurrentSchoolYear, sem = CurrentSemester, rm, daysStr, start_time, end_time, instructor_name = (instructor_name ?? "Prof. TBD").Trim(), capacity });

                await _db.LogActivityAsync(_db.CurrentUserId, $"Class offering {sec} created for {CurrentSchoolYear} by {_db.CurrentUserName}.");
                TempData["FlashMessage"] = $"Class offering {sec} successfully scheduled.";
                TempData["FlashType"] = "success";
            }
        }

        return RedirectToPage("/ClassScheduling", new { tab = "offerings" });
    }

    public async Task<IActionResult> OnPostToggleOfferingAsync(int offering_id)
    {
        _db.EnsureRegistrarSession();
        var conn = _db.Connection;
        var cur = await conn.QueryFirstOrDefaultAsync<string>("SELECT status FROM class_offerings WHERE id = @id", new { id = offering_id });
        if (!string.IsNullOrEmpty(cur))
        {
            var newStatus = cur == "open" ? "closed" : "open";
            await conn.ExecuteAsync("UPDATE class_offerings SET status = @st WHERE id = @id", new { st = newStatus, id = offering_id });
            TempData["FlashMessage"] = $"Class offering marked {newStatus}.";
            TempData["FlashType"] = "success";
        }
        return RedirectToPage("/ClassScheduling", new { tab = "offerings" });
    }

    public async Task<IActionResult> OnPostDeleteOfferingAsync(int offering_id)
    {
        _db.EnsureRegistrarSession();
        var conn = _db.Connection;
        var inUse = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM enrolled_subjects WHERE class_offering_id = @id AND status = 'enrolled'",
            new { id = offering_id });

        if (inUse > 0)
        {
            TempData["FlashMessage"] = "Cannot delete class offering: students are currently actively enrolled.";
            TempData["FlashType"] = "error";
        }
        else
        {
            await conn.ExecuteAsync("DELETE FROM class_offerings WHERE id = @id", new { id = offering_id });
            TempData["FlashMessage"] = "Class offering deleted.";
            TempData["FlashType"] = "success";
        }

        return RedirectToPage("/ClassScheduling", new { tab = "offerings" });
    }
}
