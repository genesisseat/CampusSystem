using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RegistrarMain.Models;
using RegistrarMain.Services;

namespace RegistrarMain.Pages;

public class EnrollmentValidationModel : PageModel
{
    private readonly DatabaseService _db;
    private readonly SettingsService _settings;

    public EnrollmentValidationModel(DatabaseService db, SettingsService settings)
    {
        _db = db;
        _settings = settings;
    }

    public string CurrentSchoolYear { get; set; } = "2025-2026";
    public string CurrentSemester { get; set; } = "1st Semester";
    public string ActiveTab { get; set; } = "registration";

    public List<PendingEnrollmentItem> PendingList { get; set; } = new();
    public List<PendingAddDropItem> PendingAddDrop { get; set; } = new();
    public List<PendingOverloadItem> PendingOverloads { get; set; } = new();
    public List<ActiveEnrolleeItem> ActiveList { get; set; } = new();

    public class PendingEnrollmentItem
    {
        public int Id { get; set; }
        public string SchoolYear { get; set; } = "";
        public string Semester { get; set; } = "";
        public string EnrollmentType { get; set; } = "";
        public int TotalUnits { get; set; }
        public DateTime EnrolledAt { get; set; }
        public string Name { get; set; } = "";
        public string StudentIdNumber { get; set; } = "";
        public string Email { get; set; } = "";
        public string Program { get; set; } = "";
        public string YearLevel { get; set; } = "";
        public string AcademicStatus { get; set; } = "";
        public int SubjectsCount { get; set; }
        public string FinanceClearanceStatus { get; set; } = "Cleared";
    }

    public class PendingAddDropItem
    {
        public int Id { get; set; }
        public string RequestType { get; set; } = "";
        public string? Reason { get; set; }
        public DateTime RequestedAt { get; set; }
        public string Name { get; set; } = "";
        public string StudentIdNumber { get; set; } = "";
        public string Program { get; set; } = "";
        public string? FromCode { get; set; }
        public string? FromName { get; set; }
        public string? ToCode { get; set; }
        public string? ToName { get; set; }
    }

    public class PendingOverloadItem
    {
        public int Id { get; set; }
        public string RequestType { get; set; } = "";
        public int RequestedUnits { get; set; }
        public string? Reason { get; set; }
        public DateTime RequestedAt { get; set; }
        public string Name { get; set; } = "";
        public string StudentIdNumber { get; set; } = "";
        public string Program { get; set; } = "";
        public string YearLevel { get; set; } = "";
        public decimal? AverageGrade { get; set; }
    }

    public class ActiveEnrolleeItem
    {
        public int Id { get; set; }
        public string SchoolYear { get; set; } = "";
        public string Semester { get; set; } = "";
        public string EnrollmentType { get; set; } = "";
        public int TotalUnits { get; set; }
        public DateTime EnrolledAt { get; set; }
        public int StudentId { get; set; }
        public string Name { get; set; } = "";
        public string StudentIdNumber { get; set; } = "";
        public string Program { get; set; } = "";
        public string YearLevel { get; set; } = "";
    }

    public async Task<IActionResult> OnGetAsync(string tab = "registration")
    {
        _db.EnsureRegistrarSession();
        ActiveTab = new[] { "registration", "adddrop", "overload", "active_list" }.Contains(tab) ? tab : "registration";

        CurrentSchoolYear = await _settings.GetSchoolYearAsync();
        CurrentSemester = await _settings.GetSemesterAsync();

        var conn = _db.Connection;

        // 1. Pending Enrollments
        var pEnrollments = await conn.QueryAsync<PendingEnrollmentItem>(
            "SELECT e.id, e.school_year AS SchoolYear, e.semester, e.enrollment_type AS EnrollmentType, e.total_units AS TotalUnits, e.enrolled_at AS EnrolledAt, " +
            "u.name, u.student_id_number AS StudentIdNumber, u.email, sp.program, sp.year_level AS YearLevel, sp.academic_status AS AcademicStatus, " +
            "(SELECT COUNT(*) FROM enrolled_subjects es WHERE es.enrollment_id = e.id) as SubjectsCount, " +
            "COALESCE((SELECT sc.status FROM student_clearance sc WHERE sc.student_id = e.student_id AND sc.department_name = 'Finance & Accounting Office' ORDER BY sc.id DESC LIMIT 1), 'Cleared') AS FinanceClearanceStatus " +
            "FROM enrollments e " +
            "JOIN `user` u ON u.id = e.student_id " +
            "LEFT JOIN student_profile sp ON sp.user_id = u.id " +
            "WHERE e.status = 'pending' " +
            "ORDER BY e.enrolled_at ASC");
        PendingList = pEnrollments.AsList();

        // 2. Add / Drop Petitions
        var pAddDrop = await conn.QueryAsync<PendingAddDropItem>(
            "SELECT adr.id, adr.request_type AS RequestType, adr.reason, adr.requested_at AS RequestedAt, " +
            "u.name, u.student_id_number AS StudentIdNumber, sp.program, " +
            "s_from.subject_code AS FromCode, s_from.subject_name AS FromName, " +
            "s_to.subject_code AS ToCode, s_to.subject_name AS ToName " +
            "FROM add_drop_requests adr " +
            "JOIN `user` u ON u.id = adr.student_id " +
            "LEFT JOIN student_profile sp ON sp.user_id = u.id " +
            "LEFT JOIN class_offerings co_from ON co_from.id = adr.class_offering_id " +
            "LEFT JOIN subjects s_from ON s_from.id = co_from.subject_id " +
            "LEFT JOIN class_offerings co_to ON co_to.id = adr.target_class_offering_id " +
            "LEFT JOIN subjects s_to ON s_to.id = co_to.subject_id " +
            "WHERE adr.status = 'pending' " +
            "ORDER BY adr.requested_at ASC");
        PendingAddDrop = pAddDrop.AsList();

        // 3. Overload / Waiver Petitions
        var pOverload = await conn.QueryAsync<PendingOverloadItem>(
            "SELECT owr.id, owr.request_type AS RequestType, owr.requested_units AS RequestedUnits, owr.reason, owr.requested_at AS RequestedAt, " +
            "u.name, u.student_id_number AS StudentIdNumber, sp.program, sp.year_level AS YearLevel, sp.average_grade AS AverageGrade " +
            "FROM overload_waiver_requests owr " +
            "JOIN `user` u ON u.id = owr.student_id " +
            "LEFT JOIN student_profile sp ON sp.user_id = u.id " +
            "WHERE owr.status = 'pending' " +
            "ORDER BY owr.requested_at ASC");
        PendingOverloads = pOverload.AsList();

        // 4. Active Validated Enrollments
        var aList = await conn.QueryAsync<ActiveEnrolleeItem>(
            "SELECT e.id, e.school_year AS SchoolYear, e.semester, e.enrollment_type AS EnrollmentType, e.total_units AS TotalUnits, e.enrolled_at AS EnrolledAt, " +
            "u.id as StudentId, u.name, u.student_id_number AS StudentIdNumber, sp.program, sp.year_level AS YearLevel " +
            "FROM enrollments e " +
            "JOIN `user` u ON u.id = e.student_id " +
            "LEFT JOIN student_profile sp ON sp.user_id = u.id " +
            "WHERE e.status = 'active' AND e.school_year = @sy AND e.semester = @sem " +
            "ORDER BY u.name ASC",
            new { sy = CurrentSchoolYear, sem = CurrentSemester });
        ActiveList = aList.AsList();

        return Page();
    }

    public async Task<IActionResult> OnPostEnrollmentActionAsync(int enrollment_id, string action)
    {
        _db.EnsureRegistrarSession();
        var newStatus = action == "approve_enrollment" ? "active" : "rejected";
        var conn = _db.Connection;

        var enr = await conn.QueryFirstOrDefaultAsync<(int StudentId, string SchoolYear, string Semester)>(
            "SELECT student_id AS StudentId, school_year AS SchoolYear, semester AS Semester FROM enrollments WHERE id = @id AND status = 'pending'",
            new { id = enrollment_id });

        if (enr.StudentId > 0)
        {
            await conn.OpenAsync();
            using var trans = await conn.BeginTransactionAsync();
            try
            {
                await conn.ExecuteAsync("UPDATE enrollments SET status = @st WHERE id = @id", new { st = newStatus, id = enrollment_id }, trans);
                if (newStatus == "active")
                {
                    await conn.ExecuteAsync("UPDATE student_profile SET enrollment_status = 'Active' WHERE user_id = @sid", new { sid = enr.StudentId }, trans);
                }
                await trans.CommitAsync();

                var financeNote = "";
                if (newStatus == "active")
                {
                    try
                    {
                        var studentInfo = await conn.QueryFirstOrDefaultAsync<(string FullName, string StudentNumber, string Program, int TotalUnits)>(
                            @"SELECT u.full_name AS FullName, sp.student_id_number AS StudentNumber, 
                                     sp.program AS Program, COALESCE(e.total_units, 21) AS TotalUnits
                              FROM enrollments e
                              JOIN user u ON u.id = e.student_id
                              LEFT JOIN student_profile sp ON sp.user_id = e.student_id
                              WHERE e.id = @id", new { id = enrollment_id });

                        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
                        var assessmentPayload = new
                        {
                            studentGuid = Guid.NewGuid().ToString(),
                            studentNumber = !string.IsNullOrWhiteSpace(studentInfo.StudentNumber) ? studentInfo.StudentNumber : $"2026-{enr.StudentId:D5}",
                            studentName = !string.IsNullOrWhiteSpace(studentInfo.FullName) ? studentInfo.FullName : "Enrolled Student",
                            program = !string.IsNullOrWhiteSpace(studentInfo.Program) ? studentInfo.Program : "BS Information Technology",
                            academicYear = !string.IsNullOrWhiteSpace(enr.SchoolYear) ? enr.SchoolYear : "2026-2027",
                            semester = !string.IsNullOrWhiteSpace(enr.Semester) ? enr.Semester : "1st Semester",
                            totalUnits = studentInfo.TotalUnits > 0 ? studentInfo.TotalUnits : 21
                        };

                        var response = await httpClient.PostAsJsonAsync("http://localhost:5124/api/finance/assess", assessmentPayload);
                        if (response.IsSuccessStatusCode)
                        {
                            financeNote = " &middot; Official Assessment Order (OAO) generated &amp; linked in Finance System";
                        }
                    }
                    catch
                    {
                        // Graceful degradation if Finance is temporarily unavailable
                    }
                }

                var verb = newStatus == "active" ? "approved and officially validated" : "rejected";
                await _db.LogActivityAsync(enr.StudentId, $"Enrollment for AY {enr.SchoolYear} {enr.Semester} {verb} by {_db.CurrentUserName}.");
                TempData["FlashMessage"] = $"Enrollment #{enrollment_id} has been {verb}{financeNote}.";
                TempData["FlashType"] = "success";
            }
            catch (Exception ex)
            {
                await trans.RollbackAsync();
                TempData["FlashMessage"] = "Operation failed: " + ex.Message;
                TempData["FlashType"] = "error";
            }
        }
        else
        {
            TempData["FlashMessage"] = "That enrollment record is no longer pending.";
            TempData["FlashType"] = "error";
        }

        return RedirectToPage("/EnrollmentValidation", new { tab = "registration" });
    }

    public async Task<IActionResult> OnPostAddDropActionAsync(int request_id, string action)
    {
        _db.EnsureRegistrarSession();
        var conn = _db.Connection;

        var req = await conn.QueryFirstOrDefaultAsync<AddDropRequest>(
            "SELECT * FROM add_drop_requests WHERE id = @id AND status = 'pending'",
            new { id = request_id });

        if (req == null)
        {
            TempData["FlashMessage"] = "That petition is no longer pending.";
            TempData["FlashType"] = "error";
            return RedirectToPage("/EnrollmentValidation", new { tab = "adddrop" });
        }

        if (action == "reject_adddrop")
        {
            await conn.ExecuteAsync(
                "UPDATE add_drop_requests SET status = 'rejected', processed_at = NOW(), processed_by = @regId WHERE id = @id",
                new { regId = _db.CurrentUserId, id = request_id });
            await _db.LogActivityAsync(req.StudentId, $"{req.RequestType} petition rejected by registrar.");
            TempData["FlashMessage"] = "Petition rejected.";
            TempData["FlashType"] = "success";
        }
        else
        {
            await conn.OpenAsync();
            using var trans = await conn.BeginTransactionAsync();
            try
            {
                if (new[] { "drop", "change" }.Contains(req.RequestType) && req.ClassOfferingId.HasValue)
                {
                    await conn.ExecuteAsync(
                        "UPDATE enrolled_subjects SET status = 'dropped' WHERE enrollment_id = @eid AND class_offering_id = @cid AND status = 'enrolled'",
                        new { eid = req.EnrollmentId, cid = req.ClassOfferingId.Value }, trans);
                    await conn.ExecuteAsync(
                        "UPDATE class_offerings SET slots_taken = GREATEST(slots_taken - 1, 0) WHERE id = @cid",
                        new { cid = req.ClassOfferingId.Value }, trans);
                }

                if (new[] { "add", "change" }.Contains(req.RequestType) && req.TargetClassOfferingId.HasValue)
                {
                    await conn.ExecuteAsync(
                        "INSERT INTO enrolled_subjects (enrollment_id, class_offering_id, status) VALUES (@eid, @tcid, 'enrolled')",
                        new { eid = req.EnrollmentId, tcid = req.TargetClassOfferingId.Value }, trans);
                    await conn.ExecuteAsync(
                        "UPDATE class_offerings SET slots_taken = slots_taken + 1 WHERE id = @tcid",
                        new { tcid = req.TargetClassOfferingId.Value }, trans);
                }

                await conn.ExecuteAsync(
                    "UPDATE add_drop_requests SET status = 'approved', processed_at = NOW(), processed_by = @regId WHERE id = @id",
                    new { regId = _db.CurrentUserId, id = request_id }, trans);

                await trans.CommitAsync();

                await _db.LogActivityAsync(req.StudentId, $"{req.RequestType} subject petition approved by registrar.");
                TempData["FlashMessage"] = "Petition approved and class schedule updated.";
                TempData["FlashType"] = "success";
            }
            catch (Exception ex)
            {
                await trans.RollbackAsync();
                TempData["FlashMessage"] = "Error processing petition: " + ex.Message;
                TempData["FlashType"] = "error";
            }
        }

        return RedirectToPage("/EnrollmentValidation", new { tab = "adddrop" });
    }

    public async Task<IActionResult> OnPostOverloadActionAsync(int request_id, string action)
    {
        _db.EnsureRegistrarSession();
        var newStatus = action == "approve_overload" ? "approved" : "rejected";
        var conn = _db.Connection;

        var req = await conn.QueryFirstOrDefaultAsync<(int StudentId, string RequestType)>(
            "SELECT student_id AS StudentId, request_type AS RequestType FROM overload_waiver_requests WHERE id = @id AND status = 'pending'",
            new { id = request_id });

        if (req.StudentId > 0)
        {
            await conn.ExecuteAsync(
                "UPDATE overload_waiver_requests SET status = @st, processed_at = NOW(), processed_by = @regId WHERE id = @id",
                new { st = newStatus, regId = _db.CurrentUserId, id = request_id });

            await _db.LogActivityAsync(req.StudentId, $"{req.RequestType} petition {newStatus} by registrar.");
            TempData["FlashMessage"] = $"{req.RequestType} petition {newStatus}.";
            TempData["FlashType"] = "success";
        }
        else
        {
            TempData["FlashMessage"] = "That petition is no longer pending.";
            TempData["FlashType"] = "error";
        }

        return RedirectToPage("/EnrollmentValidation", new { tab = "overload" });
    }
}
