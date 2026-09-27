using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RegistrarMain.Models;
using RegistrarMain.Services;

namespace RegistrarMain.Pages;

public class CurriculumEvaluationModel : PageModel
{
    private readonly DatabaseService _db;

    public CurriculumEvaluationModel(DatabaseService db)
    {
        _db = db;
    }

    public string ActiveTab { get; set; } = "audit";
    public List<StudentDropdownItem> StudentsList { get; set; } = new();
    public List<SubjectDropdownItem> SubjectsList { get; set; } = new();

    public int SelectedStudentId { get; set; }
    public SelectedStudentDetail? SelectedStudent { get; set; }
    public List<CurriculumProgressItem> CurriculumProgress { get; set; } = new();
    public AuditStats Stats { get; set; } = new();

    public List<CreditedRecordItem> CreditedRecords { get; set; } = new();
    public List<ShiftingRequestItem> ShiftingRequests { get; set; } = new();

    public class StudentDropdownItem
    {
        public int Id { get; set; }
        public string StudentIdNumber { get; set; } = "";
        public string Name { get; set; } = "";
        public string Program { get; set; } = "";
        public string YearLevel { get; set; } = "";
    }

    public class SubjectDropdownItem
    {
        public int Id { get; set; }
        public string SubjectCode { get; set; } = "";
        public string SubjectName { get; set; } = "";
        public decimal Units { get; set; }
    }

    public class SelectedStudentDetail
    {
        public int Id { get; set; }
        public string StudentIdNumber { get; set; } = "";
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Program { get; set; } = "";
        public string YearLevel { get; set; } = "";
        public string? CurriculumYear { get; set; } = "2024-2025";
    }

    public class CurriculumProgressItem
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public decimal Units { get; set; }
        public string? Prereq { get; set; }
        public string YearLevel { get; set; } = "";
        public string Semester { get; set; } = "";
        public string Status { get; set; } = "";
        public string Mark { get; set; } = "";
        public string Badge { get; set; } = "";
    }

    public class AuditStats
    {
        public decimal Total { get; set; }
        public decimal Completed { get; set; }
        public decimal InProgress { get; set; }
        public decimal Deficient { get; set; }
    }

    public class CreditedRecordItem
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = "";
        public string StudentIdNumber { get; set; } = "";
        public string PrevSchool { get; set; } = "";
        public string PrevSubjectCode { get; set; } = "";
        public string PrevSubjectTitle { get; set; } = "";
        public decimal PrevUnits { get; set; }
        public string PrevGrade { get; set; } = "";
        public string CreditedCode { get; set; } = "";
        public string CreditedName { get; set; } = "";
        public string EvaluatedBy { get; set; } = "";
        public string Status { get; set; } = "";
    }

    public class ShiftingRequestItem
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = "";
        public string StudentIdNumber { get; set; } = "";
        public string YearLevel { get; set; } = "";
        public string FromProgram { get; set; } = "";
        public string ToProgram { get; set; } = "";
        public decimal? AverageGrade { get; set; }
        public string? Reason { get; set; }
        public string Status { get; set; } = "pending";
    }

    public async Task<IActionResult> OnGetAsync(string tab = "audit", int student_id = 0)
    {
        _db.EnsureRegistrarSession();
        ActiveTab = new[] { "audit", "crediting", "shifting" }.Contains(tab) ? tab : "audit";

        var conn = _db.Connection;

        var students = await conn.QueryAsync<StudentDropdownItem>(
            "SELECT u.id, u.student_id_number AS StudentIdNumber, u.name, sp.program, sp.year_level AS YearLevel " +
            "FROM `user` u JOIN student_profile sp ON sp.user_id = u.id " +
            "WHERE u.role = 'student' ORDER BY u.name ASC");
        StudentsList = students.AsList();

        var subjects = await conn.QueryAsync<SubjectDropdownItem>(
            "SELECT id, subject_code AS SubjectCode, subject_name AS SubjectName, units FROM subjects ORDER BY subject_code ASC");
        SubjectsList = subjects.AsList();

        SelectedStudentId = student_id > 0 ? student_id : (StudentsList.FirstOrDefault()?.Id ?? 0);

        if (SelectedStudentId > 0)
        {
            SelectedStudent = await conn.QueryFirstOrDefaultAsync<SelectedStudentDetail>(
                "SELECT u.id, u.student_id_number AS StudentIdNumber, u.name, u.email, sp.program, sp.year_level AS YearLevel " +
                "FROM `user` u JOIN student_profile sp ON sp.user_id = u.id WHERE u.id = @id",
                new { id = SelectedStudentId });

            if (SelectedStudent != null)
            {
                var allSubs = await conn.QueryAsync<(int Id, string SubjectCode, string SubjectName, decimal Units, string YearLevel, string Semester, string? PrereqCode)>(
                    "SELECT s.id, s.subject_code AS SubjectCode, s.subject_name AS SubjectName, s.units, s.year_level AS YearLevel, s.semester, p.subject_code AS PrereqCode " +
                    "FROM subjects s LEFT JOIN subjects p ON p.id = s.prerequisite_subject_id " +
                    "ORDER BY s.year_level ASC, s.semester ASC, s.subject_code ASC");

                var earnedRows = await conn.QueryAsync<(int SubjectId, decimal? Grade, bool IsInc, string EnrollmentStatus)>(
                    "SELECT s.id AS SubjectId, g.grade AS Grade, g.is_inc AS IsInc, es.status AS EnrollmentStatus " +
                    "FROM enrolled_subjects es " +
                    "JOIN enrollments e ON e.id = es.enrollment_id " +
                    "JOIN class_offerings co ON co.id = es.class_offering_id " +
                    "JOIN subjects s ON s.id = co.subject_id " +
                    "LEFT JOIN grades g ON g.enrolled_subject_id = es.id " +
                    "WHERE e.student_id = @sid",
                    new { sid = SelectedStudentId });
                var earnedMap = earnedRows.ToDictionary(x => x.SubjectId, x => x);

                var creditedRows = await conn.QueryAsync<(int CreditedId, string PrevCode, string PrevGrade)>(
                    "SELECT credited_to_subject_id AS CreditedId, prev_subject_code AS PrevCode, prev_grade AS PrevGrade " +
                    "FROM transferee_credited_subjects WHERE student_id = @sid AND status = 'approved'",
                    new { sid = SelectedStudentId });
                var creditedMap = creditedRows.ToDictionary(x => x.CreditedId, x => x);

                foreach (var sub in allSubs)
                {
                    string status = "Deficient";
                    string mark = "—";
                    string badge = "badge-missing";

                    if (creditedMap.TryGetValue(sub.Id, out var cr))
                    {
                        status = "Credited (Transferee)";
                        mark = cr.PrevGrade;
                        badge = "badge-approved";
                        Stats.Completed += sub.Units;
                    }
                    else if (earnedMap.TryGetValue(sub.Id, out var er))
                    {
                        if (er.Grade.HasValue && er.Grade.Value <= 3.00m && !er.IsInc)
                        {
                            status = "Passed";
                            mark = er.Grade.Value.ToString("F2");
                            badge = "badge-active";
                            Stats.Completed += sub.Units;
                        }
                        else if (er.IsInc)
                        {
                            status = "Incomplete (INC)";
                            mark = "INC";
                            badge = "badge-inc";
                            Stats.InProgress += sub.Units;
                        }
                        else
                        {
                            status = "Enrolled / In-Progress";
                            mark = "Enrolled";
                            badge = "badge-open";
                            Stats.InProgress += sub.Units;
                        }
                    }
                    else
                    {
                        Stats.Deficient += sub.Units;
                    }

                    Stats.Total += sub.Units;

                    CurriculumProgress.Add(new CurriculumProgressItem
                    {
                        Code = sub.SubjectCode,
                        Name = sub.SubjectName,
                        Units = sub.Units,
                        Prereq = sub.PrereqCode,
                        YearLevel = sub.YearLevel,
                        Semester = sub.Semester,
                        Status = status,
                        Mark = mark,
                        Badge = badge
                    });
                }
            }
        }

        var creds = await conn.QueryAsync<CreditedRecordItem>(
            "SELECT tcs.id, tcs.student_id AS StudentId, u.name AS StudentName, u.student_id_number AS StudentIdNumber, " +
            "tcs.prev_school AS PrevSchool, tcs.prev_subject_code AS PrevSubjectCode, tcs.prev_subject_title AS PrevSubjectTitle, " +
            "tcs.prev_units AS PrevUnits, tcs.prev_grade AS PrevGrade, s.subject_code AS CreditedCode, s.subject_name AS CreditedName, " +
            "tcs.evaluated_by AS EvaluatedBy, tcs.status AS Status " +
            "FROM transferee_credited_subjects tcs " +
            "JOIN `user` u ON u.id = tcs.student_id " +
            "JOIN subjects s ON s.id = tcs.credited_to_subject_id " +
            "ORDER BY tcs.evaluated_at DESC");
        CreditedRecords = creds.AsList();

        var shiftings = await conn.QueryAsync<ShiftingRequestItem>(
            "SELECT csr.id, csr.student_id AS StudentId, u.name AS StudentName, u.student_id_number AS StudentIdNumber, " +
            "sp.year_level AS YearLevel, csr.from_program AS FromProgram, csr.to_program AS ToProgram, " +
            "sp.average_grade AS AverageGrade, csr.reason AS Reason, csr.status AS Status " +
            "FROM course_shifting_requests csr " +
            "JOIN `user` u ON u.id = csr.student_id " +
            "JOIN student_profile sp ON sp.user_id = u.id " +
            "ORDER BY csr.requested_at DESC");
        ShiftingRequests = shiftings.AsList();

        return Page();
    }

    public async Task<IActionResult> OnPostAccreditSubjectAsync(
        int student_id, string prev_school, string prev_subject_code, string prev_subject_title, decimal prev_units, string prev_grade, int credited_to_subject_id)
    {
        _db.EnsureRegistrarSession();
        var school = (prev_school ?? "").Trim();
        var prevCode = (prev_subject_code ?? "").Trim().ToUpper();
        var grade = (prev_grade ?? "1.50").Trim();

        if (student_id > 0 && !string.IsNullOrEmpty(school) && !string.IsNullOrEmpty(prevCode) && credited_to_subject_id > 0)
        {
            var conn = _db.Connection;
            await conn.ExecuteAsync(
                "INSERT INTO transferee_credited_subjects " +
                "(student_id, prev_school, prev_subject_code, prev_subject_title, prev_units, prev_grade, credited_to_subject_id, status, evaluated_by, evaluated_at) " +
                "VALUES (@student_id, @school, @prevCode, @prev_subject_title, @prev_units, @grade, @credited_to_subject_id, 'approved', @evalBy, NOW())",
                new { student_id, school, prevCode, prev_subject_title = (prev_subject_title ?? "").Trim(), prev_units, grade, credited_to_subject_id, evalBy = _db.CurrentUserName });

            await conn.ExecuteAsync(
                "UPDATE student_profile SET completed_units = completed_units + @u, units_remaining = GREATEST(units_remaining - @u, 0) WHERE user_id = @sid",
                new { u = (int)prev_units, sid = student_id });

            await _db.LogActivityAsync(student_id, $"Transferee subject {prevCode} accredited to institutional curriculum by {_db.CurrentUserName}.");
            TempData["FlashMessage"] = $"Transferee subject {prevCode} accredited successfully.";
            TempData["FlashType"] = "success";
        }
        else
        {
            TempData["FlashMessage"] = "Please fill in all crediting details.";
            TempData["FlashType"] = "error";
        }

        return RedirectToPage("/CurriculumEvaluation", new { tab = "crediting" });
    }

    public async Task<IActionResult> OnPostShiftingActionAsync(int request_id, string action)
    {
        _db.EnsureRegistrarSession();
        var conn = _db.Connection;

        var req = await conn.QueryFirstOrDefaultAsync<(int StudentId, string FromProgram, string ToProgram)>(
            "SELECT student_id AS StudentId, from_program AS FromProgram, to_program AS ToProgram FROM course_shifting_requests WHERE id = @id AND status = 'pending'",
            new { id = request_id });

        if (req.StudentId <= 0)
        {
            TempData["FlashMessage"] = "Shifting request is no longer pending.";
            TempData["FlashType"] = "error";
            return RedirectToPage("/CurriculumEvaluation", new { tab = "shifting" });
        }

        if (action == "reject_shifting")
        {
            await conn.ExecuteAsync(
                "UPDATE course_shifting_requests SET status = 'rejected', approved_at = NOW(), evaluated_by = @evalBy WHERE id = @id",
                new { evalBy = _db.CurrentUserName, id = request_id });
            await _db.LogActivityAsync(req.StudentId, $"Course shifting petition from {req.FromProgram} to {req.ToProgram} rejected by registrar.");
            TempData["FlashMessage"] = "Course shifting petition rejected.";
            TempData["FlashType"] = "success";
        }
        else
        {
            await conn.OpenAsync();
            using var trans = await conn.BeginTransactionAsync();
            try
            {
                await conn.ExecuteAsync("UPDATE student_profile SET program = @toProg WHERE user_id = @sid",
                    new { toProg = req.ToProgram, sid = req.StudentId }, trans);

                await conn.ExecuteAsync("UPDATE course_shifting_requests SET status = 'approved', approved_at = NOW(), evaluated_by = @evalBy WHERE id = @id",
                    new { evalBy = _db.CurrentUserName, id = request_id }, trans);

                await trans.CommitAsync();

                await _db.LogActivityAsync(req.StudentId, $"Course shifting approved: Program officially updated to {req.ToProgram} with credits migrated by {_db.CurrentUserName}.");
                TempData["FlashMessage"] = $"Course shifting approved. Student program updated to {req.ToProgram}.";
                TempData["FlashType"] = "success";
            }
            catch (Exception ex)
            {
                await trans.RollbackAsync();
                TempData["FlashMessage"] = "Error approving shifting: " + ex.Message;
                TempData["FlashType"] = "error";
            }
        }

        return RedirectToPage("/CurriculumEvaluation", new { tab = "shifting" });
    }
}
