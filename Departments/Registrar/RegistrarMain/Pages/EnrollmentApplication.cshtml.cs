using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Dapper;
using RegistrarMain.Services;
using System.Text.Json;

namespace RegistrarMain.Pages;

/// <summary>
/// Public-facing Student Enrollment Application Form — NU Lipa
/// New Freshmen and Transferee applicants only.
/// </summary>
public class EnrollmentApplicationModel : PageModel
{
    private readonly DatabaseService _db;
    private readonly IWebHostEnvironment _env;

    public EnrollmentApplicationModel(DatabaseService db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    // ── Display data ──────────────────────────────────────────────────────────
    public string SchoolYear { get; set; } = "2025-2026";
    public string Semester { get; set; } = "1st Semester";
    public List<string> Programs { get; set; } = new();
    public List<string> YearLevels { get; set; } = ["1st Year", "2nd Year", "3rd Year", "4th Year"];
    public string OfferingsByYearJson { get; set; } = "{}";
    public string CurriculumMapJson { get; set; } = "{}";

    // ── POST result ───────────────────────────────────────────────────────────
    public ApplicationResult? Success { get; set; }
    public List<string> Errors { get; set; } = new();

    // ── Static constants ──────────────────────────────────────────────────────
    private static readonly List<string> DocumentTypes =
    [
        "Form 137",
        "Form 138",
        "Birth Certificate",
        "Good Moral",
        "Transcript from Previous School",
        "Medical Clearance",
    ];

    private static readonly List<string> ProgramCatalog =
    [
        "BS Information Technology",
        "BS Computer Science",
        "BS Business Administration",
        "BS Accountancy",
        "BS Psychology",
        "BS Tourism Management",
        "BS Hospitality Management",
        "BS Criminology",
        "BS Architecture",
        "BS Nursing",
    ];

    private static readonly Dictionary<string, string> CurriculumMap = new()
    {
        ["BS Information Technology"] = "BSIT",
    };

    // ── Page Load ─────────────────────────────────────────────────────────────
    public async Task OnGetAsync()
    {
        await LoadDropdownsAsync();
    }

    // ── Form Submit ───────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostAsync(
        string enrollment_type,
        string full_name,
        string email,
        string contact_number,
        string gender,
        string birth_date,
        string address,
        string program,
        string year_level,
        string? previous_school,
        [FromForm(Name = "class_offering_ids[]")] List<int>? class_offering_ids)
    {
        await LoadDropdownsAsync();

        var enrollmentType = enrollment_type is "new" or "transferee" ? enrollment_type : "new";
        if (!new[] { "Male", "Female" }.Contains(gender)) gender = "Female";
        if (!YearLevels.Contains(year_level)) year_level = "1st Year";
        if (!Programs.Contains(program))
            Errors.Add("Please select a valid degree program.");

        if (string.IsNullOrWhiteSpace(full_name)) Errors.Add("Full name is required.");
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@')) Errors.Add("A valid email address is required.");
        if (string.IsNullOrWhiteSpace(contact_number)) Errors.Add("Contact number is required.");
        if (string.IsNullOrWhiteSpace(birth_date) || !DateOnly.TryParse(birth_date, out _)) Errors.Add("Date of birth is required.");
        if (string.IsNullOrWhiteSpace(address)) Errors.Add("Home address is required.");
        if (enrollmentType == "transferee" && string.IsNullOrWhiteSpace(previous_school))
            Errors.Add("Please indicate your previous school for a transferee application.");

        if (Errors.Count == 0)
        {
            var conn = _db.Connection;
            var dup = await conn.QueryFirstOrDefaultAsync<int?>(
                "SELECT id FROM `user` WHERE email = @email", new { email });
            if (dup.HasValue)
                Errors.Add("An account with that email address already exists. Please contact the Registrar's Office if this is a mistake.");
        }

        // Server-side re-validation of chosen class offerings
        var validOfferings = new List<OfferingMeta>();
        if (Errors.Count == 0 && class_offering_ids != null && class_offering_ids.Count > 0)
        {
            var conn = _db.Connection;
            var inSet = string.Join(",", class_offering_ids);
            var offerings = await conn.QueryAsync<OfferingMeta>(
                $"SELECT co.id as Id, co.capacity as Capacity, co.slots_taken as SlotsTaken, " +
                $"co.status as Status, s.units as Units, s.subject_code as SubjectCode " +
                $"FROM class_offerings co " +
                $"JOIN subjects s ON s.id = co.subject_id " +
                $"WHERE co.id IN ({inSet}) AND co.school_year = @sy AND co.semester = @sem",
                new { sy = SchoolYear, sem = Semester });

            validOfferings = offerings
                .Where(o => o.Status == "open" && o.SlotsTaken < o.Capacity)
                .ToList();
        }

        if (Errors.Count == 0)
        {
            try
            {
                var conn = _db.Connection;
                await conn.OpenAsync();
                await using var tx = await conn.BeginTransactionAsync();

                // 1. Generate student ID number
                var yearLevelIndex = YearLevels.IndexOf(year_level);
                var syStart = int.Parse(SchoolYear[..4]);
                var entryYear = syStart - yearLevelIndex;

                var seqPrefix = entryYear.ToString();
                var lastId = await conn.QueryFirstOrDefaultAsync<string>(
                    "SELECT student_id_number FROM `user` WHERE student_id_number LIKE @prefix ORDER BY student_id_number DESC LIMIT 1",
                    new { prefix = seqPrefix + "-%" }, transaction: tx);
                var nextSeq = lastId != null ? int.Parse(lastId[5..]) + 1 : 1;
                var studentIdNumber = $"{seqPrefix}-{nextSeq:D5}";

                // 2. Temp password
                var tempPassword = $"NULipa-{Random.Shared.Next(1000, 9999)}";
                var hashed = BCrypt.Net.BCrypt.HashPassword(tempPassword);

                await conn.ExecuteAsync(
                    "INSERT INTO `user` (student_id_number, name, email, password, role, status) VALUES (@sid, @name, @email, @pw, 'student', 'active')",
                    new { sid = studentIdNumber, name = full_name, email, pw = hashed }, transaction: tx);
                var newUserId = await conn.ExecuteScalarAsync<int>("SELECT LAST_INSERT_ID()", transaction: tx);

                // 3. Student profile
                var guidanceNote = enrollmentType == "transferee"
                    ? $"Transferee applicant from {previous_school}; awaiting initial guidance interview."
                    : "New applicant; awaiting initial guidance interview.";
                var curriculumYear = $"{entryYear}-{entryYear + 4}";

                await conn.ExecuteAsync(
                    "INSERT INTO student_profile (user_id, program, year_level, curriculum_year, academic_status, enrollment_status, " +
                    "completed_units, units_remaining, guidance_clearance_status, guidance_notes, contact_number, address, birth_date, gender) " +
                    "VALUES (@uid, @prog, @yl, @cy, 'Good Standing', 'Active', 0, 144, 'Pending', @gn, @cn, @addr, @bd, @gender)",
                    new { uid = newUserId, prog = program, yl = year_level, cy = curriculumYear,
                          gn = guidanceNote, cn = contact_number, addr = address, bd = birth_date, gender },
                    transaction: tx);

                // 4. Enrollment record
                var totalUnits = validOfferings.Sum(o => o.Units);
                await conn.ExecuteAsync(
                    "INSERT INTO enrollments (student_id, school_year, semester, enrollment_type, status, total_units) VALUES (@sid, @sy, @sem, @et, 'pending', @tu)",
                    new { sid = newUserId, sy = SchoolYear, sem = Semester, et = enrollmentType, tu = totalUnits },
                    transaction: tx);
                var newEnrollmentId = await conn.ExecuteScalarAsync<int>("SELECT LAST_INSERT_ID()", transaction: tx);

                // 5. Enrolled subjects + slot bump
                var chosenSubjects = new List<string>();
                foreach (var off in validOfferings)
                {
                    await conn.ExecuteAsync(
                        "INSERT INTO enrolled_subjects (enrollment_id, class_offering_id, status) VALUES (@eid, @oid, 'enrolled')",
                        new { eid = newEnrollmentId, oid = off.Id }, transaction: tx);
                    await conn.ExecuteAsync(
                        "UPDATE class_offerings SET slots_taken = slots_taken + 1 WHERE id = @id",
                        new { id = off.Id }, transaction: tx);
                    chosenSubjects.Add(off.SubjectCode);
                }

                // 6. Seed 201 document checklist
                foreach (var docType in DocumentTypes)
                {
                    string? remark = docType == "Transcript from Previous School"
                        ? (enrollmentType == "transferee"
                            ? $"Awaiting official TOR from {previous_school}."
                            : "Not applicable (Direct High School Graduate entry).")
                        : null;
                    await conn.ExecuteAsync(
                        "INSERT IGNORE INTO document_credentials (student_id, document_type, status, remarks) VALUES (@uid, @dt, 'Missing', @r)",
                        new { uid = newUserId, dt = docType, r = remark }, transaction: tx);
                }

                // 7. Save uploaded documents and update file_path
                var docFileMap = new Dictionary<string, string>
                {
                    ["doc_Form137"]          = "Form 137",
                    ["doc_Form138"]          = "Form 138",
                    ["doc_BirthCertificate"] = "Birth Certificate",
                    ["doc_GoodMoral"]        = "Good Moral",
                    ["doc_Transcript"]       = "Transcript from Previous School",
                    ["doc_MedicalClearance"] = "Medical Clearance",
                };

                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "documents", newUserId.ToString());
                Directory.CreateDirectory(uploadsDir);

                foreach (var (fieldName, docType) in docFileMap)
                {
                    var file = Request.Form.Files[fieldName];
                    if (file is { Length: > 0 })
                    {
                        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                        var safeDocName = docType.Replace(" ", "_");
                        var fileName = $"{safeDocName}{ext}";
                        var fullPath = Path.Combine(uploadsDir, fileName);
                        using var stream = System.IO.File.Create(fullPath);
                        await file.CopyToAsync(stream);
                        var relPath = $"uploads/documents/{newUserId}/{fileName}";
                        await conn.ExecuteAsync(
                            "UPDATE document_credentials SET file_path = @fp, status = 'Submitted', submitted_at = NOW() " +
                            "WHERE student_id = @uid AND document_type = @dt",
                            new { fp = relPath, uid = newUserId, dt = docType }, transaction: tx);
                    }
                }

                await _db.LogActivityAsync(newUserId,
                    $"new RegistrarEnrollment application submitted online: {full_name} ({studentIdNumber}), {program}, {year_level}, AY {SchoolYear} {Semester} — pending Registrar validation.");

                await tx.CommitAsync();

                Success = new ApplicationResult
                {
                    StudentIdNumber = studentIdNumber,
                    PortalEmail = email,
                    TempPassword = tempPassword,
                    Program = program,
                    YearLevel = year_level,
                    EnrollmentType = enrollmentType,
                    TotalUnits = totalUnits,
                    Subjects = chosenSubjects,
                    SchoolYear = SchoolYear,
                    Semester = Semester,
                };

                return Page();
            }
            catch (Exception ex)
            {
                Errors.Add("We could not process your application: " + ex.Message);
            }
        }

        return Page();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private async Task LoadDropdownsAsync()
    {
        var conn = _db.Connection;
        SchoolYear = await _db.GetSettingAsync("current_school_year", "2025-2026");
        Semester = await _db.GetSettingAsync("current_semester", "1st Semester");

        var dbPrograms = (await conn.QueryAsync<string>(
            "SELECT DISTINCT program FROM student_profile WHERE program IS NOT NULL AND program != '' ORDER BY program"
        )).ToList();

        Programs = ProgramCatalog.Union(dbPrograms).Distinct().OrderBy(p => p).ToList();

        // Pre-load class offerings grouped by year_level for client-side JS rendering
        var rows = await conn.QueryAsync<OfferingRow>(
            "SELECT co.id, co.section_code, co.room, co.days_of_week, co.start_time, co.end_time, " +
            "co.instructor_name, co.capacity, co.slots_taken, co.status, " +
            "s.subject_code, s.subject_name, s.units, s.year_level, s.curriculum_program " +
            "FROM class_offerings co " +
            "JOIN subjects s ON s.id = co.subject_id " +
            "WHERE co.school_year = @sy AND co.semester = @sem AND s.curriculum_program IN ('BSIT', 'All') " +
            "ORDER BY s.year_level, s.subject_code",
            new { sy = SchoolYear, sem = Semester });

        var grouped = new Dictionary<string, List<object>>();
        foreach (var r in rows)
        {
            if (!grouped.ContainsKey(r.year_level)) grouped[r.year_level] = new();
            grouped[r.year_level].Add(new
            {
                id = r.id,
                code = r.subject_code,
                name = r.subject_name,
                units = r.units,
                section = r.section_code,
                room = r.room,
                schedule = $"{r.days_of_week} {DateTime.Today.Add(r.start_time):hh\\:mm tt}-{DateTime.Today.Add(r.end_time):hh\\:mm tt}",
                instructor = r.instructor_name,
                capacity = r.capacity,
                taken = r.slots_taken,
                status = r.status,
            });
        }

        OfferingsByYearJson = JsonSerializer.Serialize(grouped);
        CurriculumMapJson = JsonSerializer.Serialize(CurriculumMap);
    }

    // ── Inner types ───────────────────────────────────────────────────────────
    private class OfferingRow
    {
        public int id { get; set; }
        public string section_code { get; set; } = "";
        public string room { get; set; } = "";
        public string days_of_week { get; set; } = "";
        public TimeSpan start_time { get; set; }
        public TimeSpan end_time { get; set; }
        public string instructor_name { get; set; } = "";
        public int capacity { get; set; }
        public int slots_taken { get; set; }
        public string status { get; set; } = "";
        public string subject_code { get; set; } = "";
        public string subject_name { get; set; } = "";
        public float units { get; set; }
        public string year_level { get; set; } = "";
        public string curriculum_program { get; set; } = "";
    }

    private class OfferingMeta
    {
        public int Id { get; set; }
        public int Capacity { get; set; }
        public int SlotsTaken { get; set; }
        public string Status { get; set; } = "";
        public float Units { get; set; }
        public string SubjectCode { get; set; } = "";
    }

    public class ApplicationResult
    {
        public string StudentIdNumber { get; set; } = "";
        public string PortalEmail { get; set; } = "";
        public string TempPassword { get; set; } = "";
        public string Program { get; set; } = "";
        public string YearLevel { get; set; } = "";
        public string EnrollmentType { get; set; } = "";
        public float TotalUnits { get; set; }
        public List<string> Subjects { get; set; } = new();
        public string SchoolYear { get; set; } = "";
        public string Semester { get; set; } = "";
    }
}
