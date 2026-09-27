using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RegistrarMain.Models;
using RegistrarMain.Services;

namespace RegistrarMain.Pages;

public class GradeControlModel : PageModel
{
    private readonly DatabaseService _db;
    private readonly SettingsService _settings;

    public GradeControlModel(DatabaseService db, SettingsService settings)
    {
        _db = db;
        _settings = settings;
    }

    public string CurrentSchoolYear { get; set; } = "2025-2026";
    public string CurrentSemester { get; set; } = "1st Semester";
    public bool EncodingOpen { get; set; } = true;
    public string EncodingDeadline { get; set; } = "2025-10-30";
    public string ActiveTab { get; set; } = "encoding";

    public List<OfferingSimpleItem> OfferingsList { get; set; } = new();
    public int SelectedOfferingId { get; set; }
    public OfferingDetailItem? SelectedOffering { get; set; }
    public List<GradeSheetRow> GradeSheet { get; set; } = new();
    public List<PendingRevisionItem> PendingRevisions { get; set; } = new();

    public class OfferingSimpleItem
    {
        public int Id { get; set; }
        public string SectionCode { get; set; } = "";
        public string Room { get; set; } = "";
        public string SubjectCode { get; set; } = "";
        public string SubjectName { get; set; } = "";
        public string InstructorName { get; set; } = "";
    }

    public class OfferingDetailItem
    {
        public int Id { get; set; }
        public string SectionCode { get; set; } = "";
        public string SubjectCode { get; set; } = "";
        public string SubjectName { get; set; } = "";
        public decimal Units { get; set; }
        public string InstructorName { get; set; } = "";
    }

    public class GradeSheetRow
    {
        public int EnrolledSubId { get; set; }
        public int StudentId { get; set; }
        public string Name { get; set; } = "";
        public string StudentIdNumber { get; set; } = "";
        public string Program { get; set; } = "";
        public string YearLevel { get; set; } = "";
        public int? GradeId { get; set; }
        public decimal? Grade { get; set; }
        public bool IsInc { get; set; }
        public string? GradeStatus { get; set; }
        public string? Remarks { get; set; }
    }

    public class PendingRevisionItem
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = "";
        public string StudentIdNumber { get; set; } = "";
        public int GradeId { get; set; }
        public string SubjectCode { get; set; } = "";
        public string SubjectName { get; set; } = "";
        public decimal? CurrentGrade { get; set; }
        public bool CurrentIsInc { get; set; }
        public decimal? RequestedGrade { get; set; }
        public string? Reason { get; set; }
        public DateTime RequestedAt { get; set; }
    }

    private async Task RecalculateStudentGwaAsync(int studentId)
    {
        var conn = _db.Connection;
        var rows = await conn.QueryAsync<(decimal Grade, int Units)>(
            "SELECT g.grade AS Grade, s.units AS Units " +
            "FROM enrolled_subjects es " +
            "JOIN enrollments e ON e.id = es.enrollment_id " +
            "JOIN class_offerings co ON co.id = es.class_offering_id " +
            "JOIN subjects s ON s.id = co.subject_id " +
            "JOIN grades g ON g.enrolled_subject_id = es.id " +
            "WHERE e.student_id = @sid AND g.status IN ('verified', 'locked') AND g.is_inc = 0 AND g.grade IS NOT NULL",
            new { sid = studentId });

        if (rows.Any())
        {
            decimal totalQualityPoints = 0;
            decimal totalUnits = 0;
            foreach (var r in rows)
            {
                totalQualityPoints += (r.Grade * r.Units);
                totalUnits += r.Units;
            }
            decimal? gwa = totalUnits > 0 ? Math.Round(totalQualityPoints / totalUnits, 2) : null;
            await conn.ExecuteAsync("UPDATE student_profile SET average_grade = @gwa WHERE user_id = @sid", new { gwa, sid = studentId });
        }
    }

    public async Task<IActionResult> OnGetAsync(string tab = "encoding", int offering_id = 0)
    {
        _db.EnsureRegistrarSession();
        ActiveTab = new[] { "encoding", "verification", "revisions" }.Contains(tab) ? tab : "encoding";

        CurrentSchoolYear = await _settings.GetSchoolYearAsync();
        CurrentSemester = await _settings.GetSemesterAsync();
        EncodingOpen = await _settings.IsEncodingOpenAsync();
        EncodingDeadline = await _settings.GetEncodingDeadlineAsync();

        var conn = _db.Connection;

        var offerings = await conn.QueryAsync<OfferingSimpleItem>(
            "SELECT co.id, co.section_code AS SectionCode, co.room, s.subject_code AS SubjectCode, s.subject_name AS SubjectName, co.instructor_name AS InstructorName " +
            "FROM class_offerings co " +
            "JOIN subjects s ON s.id = co.subject_id " +
            "ORDER BY co.section_code ASC, s.subject_code ASC");
        OfferingsList = offerings.AsList();

        SelectedOfferingId = offering_id > 0 ? offering_id : (OfferingsList.FirstOrDefault()?.Id ?? 0);

        if (SelectedOfferingId > 0)
        {
            SelectedOffering = await conn.QueryFirstOrDefaultAsync<OfferingDetailItem>(
                "SELECT co.id, co.section_code AS SectionCode, s.subject_code AS SubjectCode, s.subject_name AS SubjectName, s.units, co.instructor_name AS InstructorName " +
                "FROM class_offerings co JOIN subjects s ON s.id = co.subject_id WHERE co.id = @id",
                new { id = SelectedOfferingId });

            var sheet = await conn.QueryAsync<GradeSheetRow>(
                "SELECT es.id AS EnrolledSubId, u.id AS StudentId, u.name, u.student_id_number AS StudentIdNumber, sp.program, sp.year_level AS YearLevel, " +
                "g.id AS GradeId, g.grade, g.is_inc AS IsInc, g.status AS GradeStatus, g.remarks " +
                "FROM enrolled_subjects es " +
                "JOIN enrollments e ON e.id = es.enrollment_id " +
                "JOIN `user` u ON u.id = e.student_id " +
                "LEFT JOIN student_profile sp ON sp.user_id = u.id " +
                "LEFT JOIN grades g ON g.enrolled_subject_id = es.id " +
                "WHERE es.class_offering_id = @id AND es.status = 'enrolled' " +
                "ORDER BY u.name ASC",
                new { id = SelectedOfferingId });
            GradeSheet = sheet.AsList();
        }

        var revisions = await conn.QueryAsync<PendingRevisionItem>(
            "SELECT crr.id, crr.student_id AS StudentId, u.name AS StudentName, u.student_id_number AS StudentIdNumber, crr.grade_id AS GradeId, " +
            "s.subject_code AS SubjectCode, s.subject_name AS SubjectName, g.grade AS CurrentGrade, g.is_inc AS CurrentIsInc, " +
            "crr.requested_grade AS RequestedGrade, crr.reason, crr.requested_at AS RequestedAt " +
            "FROM completion_revision_requests crr " +
            "JOIN `user` u ON u.id = crr.student_id " +
            "JOIN grades g ON g.id = crr.grade_id " +
            "JOIN enrolled_subjects es ON es.id = g.enrolled_subject_id " +
            "JOIN class_offerings co ON co.id = es.class_offering_id " +
            "JOIN subjects s ON s.id = co.subject_id " +
            "WHERE crr.status = 'pending' " +
            "ORDER BY crr.requested_at ASC");
        PendingRevisions = revisions.AsList();

        return Page();
    }

    public async Task<IActionResult> OnPostSaveWindowAsync(string? window_open, string? deadline)
    {
        _db.EnsureRegistrarSession();
        var open = window_open == "1" ? "1" : "0";
        var dead = (deadline ?? "").Trim();

        await _db.SetSettingAsync("grade_encoding_open", open);
        await _db.SetSettingAsync("grade_encoding_deadline", dead);

        await _db.LogActivityAsync(_db.CurrentUserId, $"Grade encoding window set to {(open == "1" ? "OPEN" : "CLOSED")} by {_db.CurrentUserName}.");
        TempData["FlashMessage"] = "Grade encoding window settings updated.";
        TempData["FlashType"] = "success";

        return RedirectToPage("/GradeControl", new { tab = "encoding" });
    }

    public async Task<IActionResult> OnPostBatchSaveGradesAsync(int offering_id, IFormCollection form)
    {
        _db.EnsureRegistrarSession();
        EncodingOpen = await _settings.IsEncodingOpenAsync();

        if (!EncodingOpen)
        {
            TempData["FlashMessage"] = "Grade encoding window is currently CLOSED.";
            TempData["FlashType"] = "error";
            return RedirectToPage("/GradeControl", new { tab = "encoding", offering_id });
        }

        var conn = _db.Connection;
        await conn.OpenAsync();
        using var trans = await conn.BeginTransactionAsync();
        try
        {
            foreach (var key in form.Keys.Where(k => k.StartsWith("grades[")))
            {
                var idStr = key.Substring(7, key.Length - 8);
                if (!int.TryParse(idStr, out var enrolledSubId)) continue;

                var isInc = form.ContainsKey($"is_inc[{enrolledSubId}]") && form[$"is_inc[{enrolledSubId}]"] == "1";
                var gradeStr = form[key].ToString().Trim();

                decimal? gradeToStore = null;
                string remarks;

                if (isInc)
                {
                    gradeToStore = null;
                    remarks = "Incomplete (INC)";
                }
                else if (!string.IsNullOrEmpty(gradeStr))
                {
                    if (!decimal.TryParse(gradeStr, out var numGrade) || numGrade < 1.00m || numGrade > 5.00m) continue;
                    gradeToStore = numGrade;
                    remarks = numGrade <= 3.00m ? "Passed" : "Failed";
                }
                else
                {
                    continue;
                }

                var existing = await conn.QueryFirstOrDefaultAsync<(int Id, string Status)>(
                    "SELECT id, status FROM grades WHERE enrolled_subject_id = @sid", new { sid = enrolledSubId }, trans);

                if (existing.Id > 0)
                {
                    if (existing.Status == "locked") continue;
                    await conn.ExecuteAsync(
                        "UPDATE grades SET grade = @g, is_inc = @inc, status = 'draft', remarks = @rem, encoded_by = @uid, encoded_at = NOW() WHERE id = @id",
                        new { g = gradeToStore, inc = isInc ? 1 : 0, rem = remarks, uid = _db.CurrentUserId, id = existing.Id }, trans);
                }
                else
                {
                    await conn.ExecuteAsync(
                        "INSERT INTO grades (enrolled_subject_id, grade, is_inc, status, remarks, encoded_by, encoded_at) " +
                        "VALUES (@sid, @g, @inc, 'draft', @rem, @uid, NOW())",
                        new { sid = enrolledSubId, g = gradeToStore, inc = isInc ? 1 : 0, rem = remarks, uid = _db.CurrentUserId }, trans);
                }
            }

            await trans.CommitAsync();
            await _db.LogActivityAsync(_db.CurrentUserId, $"Grades encoded for offering #{offering_id} by {_db.CurrentUserName}.");
            TempData["FlashMessage"] = "Section grades saved as Draft.";
            TempData["FlashType"] = "success";
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();
            TempData["FlashMessage"] = "Error saving grades: " + ex.Message;
            TempData["FlashType"] = "error";
        }

        return RedirectToPage("/GradeControl", new { tab = "encoding", offering_id });
    }

    public async Task<IActionResult> OnPostVerifyOrLockAsync(int offering_id, string action)
    {
        _db.EnsureRegistrarSession();
        var newStatus = action == "verify_section" ? "verified" : "locked";
        var conn = _db.Connection;

        var gradesToUpdate = await conn.QueryAsync<(int GradeId, int StudentId)>(
            "SELECT g.id AS GradeId, e.student_id AS StudentId " +
            "FROM grades g " +
            "JOIN enrolled_subjects es ON es.id = g.enrolled_subject_id " +
            "JOIN enrollments e ON e.id = es.enrollment_id " +
            "WHERE es.class_offering_id = @cid",
            new { cid = offering_id });

        await conn.OpenAsync();
        using var trans = await conn.BeginTransactionAsync();
        try
        {
            foreach (var gu in gradesToUpdate)
            {
                if (action == "verify_section")
                {
                    await conn.ExecuteAsync(
                        "UPDATE grades SET status = 'verified', verified_by = @uid, verified_at = NOW() WHERE id = @id AND status = 'draft'",
                        new { uid = _db.CurrentUserId, id = gu.GradeId }, trans);
                }
                else
                {
                    await conn.ExecuteAsync(
                        "UPDATE grades SET status = 'locked', locked_at = NOW() WHERE id = @id AND status IN ('draft', 'verified')",
                        new { id = gu.GradeId }, trans);
                }
            }
            await trans.CommitAsync();

            foreach (var gu in gradesToUpdate)
            {
                await RecalculateStudentGwaAsync(gu.StudentId);
            }

            await _db.LogActivityAsync(_db.CurrentUserId, $"Grades {newStatus} for class offering #{offering_id} by {_db.CurrentUserName}.");
            TempData["FlashMessage"] = $"Class offering grades {newStatus} and student GWAs recalculated.";
            TempData["FlashType"] = "success";
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();
            TempData["FlashMessage"] = "Error updating grades: " + ex.Message;
            TempData["FlashType"] = "error";
        }

        return RedirectToPage("/GradeControl", new { tab = "verification", offering_id });
    }

    public async Task<IActionResult> OnPostRevisionActionAsync(int request_id, string action)
    {
        _db.EnsureRegistrarSession();
        var conn = _db.Connection;

        var req = await conn.QueryFirstOrDefaultAsync<CompletionRevisionRequest>(
            "SELECT * FROM completion_revision_requests WHERE id = @id AND status = 'pending'",
            new { id = request_id });

        if (req == null)
        {
            TempData["FlashMessage"] = "Petition is no longer pending.";
            TempData["FlashType"] = "error";
            return RedirectToPage("/GradeControl", new { tab = "revisions" });
        }

        if (action == "reject_revision")
        {
            await conn.ExecuteAsync(
                "UPDATE completion_revision_requests SET status = 'rejected', processed_at = NOW(), processed_by = @uid WHERE id = @id",
                new { uid = _db.CurrentUserId, id = request_id });
            await _db.LogActivityAsync(req.StudentId, "Grade revision petition rejected by registrar.");
            TempData["FlashMessage"] = "Grade revision petition rejected.";
            TempData["FlashType"] = "success";
        }
        else
        {
            await conn.OpenAsync();
            using var trans = await conn.BeginTransactionAsync();
            try
            {
                var rem = req.ProposedGrade.HasValue && req.ProposedGrade.Value <= 3.00m ? "Passed (INC Completed)" : "Failed";
                await conn.ExecuteAsync(
                    "UPDATE grades SET grade = @g, is_inc = 0, status = 'verified', remarks = @rem, verified_by = @uid, verified_at = NOW() WHERE id = @gid",
                    new { g = req.ProposedGrade, rem, uid = _db.CurrentUserId, gid = req.GradeId }, trans);

                await conn.ExecuteAsync(
                    "UPDATE completion_revision_requests SET status = 'approved', processed_at = NOW(), processed_by = @uid WHERE id = @id",
                    new { uid = _db.CurrentUserId, id = request_id }, trans);

                await trans.CommitAsync();

                await RecalculateStudentGwaAsync(req.StudentId);
                await _db.LogActivityAsync(req.StudentId, $"Grade completion approved: final grade set to {req.ProposedGrade} by {_db.CurrentUserName}.");
                TempData["FlashMessage"] = "Grade completion approved and applied to permanent record.";
                TempData["FlashType"] = "success";
            }
            catch (Exception ex)
            {
                await trans.RollbackAsync();
                TempData["FlashMessage"] = "Error approving petition: " + ex.Message;
                TempData["FlashType"] = "error";
            }
        }

        return RedirectToPage("/GradeControl", new { tab = "revisions" });
    }
}
