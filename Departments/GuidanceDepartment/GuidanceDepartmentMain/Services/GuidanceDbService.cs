using Dapper;
using MySqlConnector;

namespace GuidanceDepartmentMain.Services;

/// <summary>
/// Provides MySQL access to the shared campus database (mydb) for the Guidance Department.
/// Reads enrolled students from the shared <c>user</c> table and writes clearance decisions
/// to <c>student_clearance</c> so StudentPortal and Registrar reflect them in real-time.
/// </summary>
public interface IGuidanceStudentDirectory
{
    Task<IReadOnlyList<GuidanceDbService.StudentRow>> GetStudentsAsync();
}

public sealed class GuidanceDbService : IGuidanceStudentDirectory
{
    private readonly string _connectionString;
    private readonly ILogger<GuidanceDbService> _logger;

    public GuidanceDbService(IConfiguration config, ILogger<GuidanceDbService> logger)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is required.");
        _logger = logger;
    }

    private MySqlConnection CreateConnection() => new(_connectionString);

    // -------------------------------------------------------------------------
    // Schema bootstrap
    // -------------------------------------------------------------------------

    /// <summary>
    /// Ensures Guidance-specific MySQL tables exist (called once on startup).
    /// The shared tables (student_clearance, user) are already managed by Finance / StudentPortal.
    /// </summary>
    public async Task EnsureSchemaAsync()
    {
        try
        {
            await using var db = CreateConnection();
            await db.OpenAsync();

            await db.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS `guidance_notes` (
                    `id` INT AUTO_INCREMENT PRIMARY KEY,
                    `student_id` INT NOT NULL,
                    `counselor` VARCHAR(150) NOT NULL,
                    `note_date` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    `note_type` VARCHAR(60) NOT NULL DEFAULT 'General',
                    `content` TEXT NOT NULL,
                    `is_confidential` TINYINT(1) NOT NULL DEFAULT 1,
                    INDEX `idx_gn_student` (`student_id`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

            await db.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS `guidance_appointments` (
                    `id` INT AUTO_INCREMENT PRIMARY KEY,
                    `student_id` INT NOT NULL,
                    `counselor` VARCHAR(150) NOT NULL DEFAULT 'Unassigned',
                    `appointment_dt` DATETIME NOT NULL,
                    `status` VARCHAR(50) NOT NULL DEFAULT 'Scheduled',
                    `purpose` VARCHAR(255) NOT NULL DEFAULT 'General Counseling',
                    `remarks` TEXT NULL,
                    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    INDEX `idx_ga_student` (`student_id`),
                    INDEX `idx_ga_dt` (`appointment_dt`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

            await db.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS `guidance_requests` (
                    `id` CHAR(36) NOT NULL PRIMARY KEY,
                    `student_id` INT NOT NULL,
                    `subject` VARCHAR(255) NOT NULL,
                    `details` TEXT NOT NULL,
                    `safety_valve_text` TEXT NULL,
                    `urgency` INT NOT NULL,
                    `status` INT NOT NULL,
                    `assigned_counselor_id` CHAR(36) NULL,
                    `idempotency_key` VARCHAR(191) NULL,
                    `row_version` BINARY(8) NOT NULL,
                    UNIQUE KEY `uniq_guidance_request_idempotency` (`student_id`, `idempotency_key`),
                    INDEX `idx_guidance_request_status` (`status`, `urgency`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

            await db.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS `refresh_tokens` (
                    `token` VARCHAR(128) NOT NULL PRIMARY KEY,
                    `subject` VARCHAR(255) NOT NULL,
                    `expires_at` DATETIME(6) NOT NULL,
                    INDEX `idx_refresh_tokens_expiry` (`expires_at`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

            _logger.LogInformation("Guidance MySQL schema verified successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not initialize the Guidance MySQL schema.");
            throw;
        }
    }

    // -------------------------------------------------------------------------
    // Student queries (from shared `user` table)
    // -------------------------------------------------------------------------

    public sealed record StudentRow(int Id, string StudentIdNumber, string Name, string Email);

    /// <summary>
    /// Returns all active enrolled students from the shared MySQL <c>user</c> table.
    /// This is the authoritative source — the same list seen by Registrar, Finance, and StudentPortal.
    /// </summary>
    public async Task<IReadOnlyList<StudentRow>> GetStudentsAsync()
    {
        try
        {
            await using var db = CreateConnection();
            var rows = await db.QueryAsync<StudentRow>(@"
                SELECT id             AS Id,
                       student_id_number AS StudentIdNumber,
                       name           AS Name,
                       email          AS Email
                FROM   `user`
                WHERE  role   = 'student'
                  AND  status = 'active'
                ORDER BY name;");
            return rows.AsList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not load students from the shared MySQL database.");
            throw;
        }
    }

    /// <summary>Looks up a student by their student ID number string (e.g. "2024-00001").</summary>
    public async Task<StudentRow?> FindStudentByNumberAsync(string studentIdNumber)
    {
        try
        {
            await using var db = CreateConnection();
            return await db.QueryFirstOrDefaultAsync<StudentRow>(@"
                SELECT id             AS Id,
                       student_id_number AS StudentIdNumber,
                       name           AS Name,
                       email          AS Email
                FROM   `user`
                WHERE  student_id_number = @studentIdNumber
                  AND  role = 'student'
                LIMIT  1;",
                new { studentIdNumber });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not find student {StudentIdNumber} in the shared MySQL database.", studentIdNumber);
            throw;
        }
    }

    /// <summary>Resolves numeric MySQL user.id from a student ID number string.</summary>
    public async Task<int?> FindStudentIdByNumberAsync(string studentIdNumber)
    {
        var row = await FindStudentByNumberAsync(studentIdNumber);
        return row?.Id;
    }

    // -------------------------------------------------------------------------
    // Clearance writes (to shared `student_clearance` table)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Upserts a Guidance &amp; Counseling Office clearance row into the shared MySQL
    /// <c>student_clearance</c> table so StudentPortal and Registrar see it immediately.
    /// </summary>
    /// <param name="studentId">Numeric MySQL <c>user.id</c> (NOT the student ID number string).</param>
    /// <param name="schoolYear">e.g. "2026-2027"</param>
    /// <param name="semester">e.g. "1st Semester"</param>
    /// <param name="status">"Cleared" | "Pending" | "Hold"</param>
    /// <param name="clearedBy">Name of the counselor issuing the clearance.</param>
    /// <param name="remarks">Optional remarks visible in StudentPortal.</param>
    public async Task<bool> UpsertGuidanceClearanceAsync(
        int studentId,
        string schoolYear,
        string semester,
        string status,
        string clearedBy = "Guidance Office",
        string? remarks = null)
    {
        try
        {
            await using var db = CreateConnection();
            await db.OpenAsync();

            await db.ExecuteAsync(@"
                INSERT INTO `student_clearance`
                    (`student_id`, `department_name`, `school_year`, `semester`,
                     `status`, `cleared_by`, `cleared_at`, `remarks`)
                VALUES
                    (@studentId, 'Guidance & Counseling Office', @schoolYear, @semester,
                     @status, @clearedBy, NOW(), @remarks)
                ON DUPLICATE KEY UPDATE
                    `status`     = @status,
                    `cleared_by` = @clearedBy,
                    `cleared_at` = NOW(),
                    `remarks`    = @remarks;",
                new { studentId, schoolYear, semester, status, clearedBy, remarks });

            _logger.LogInformation(
                "Guidance clearance upserted for student {StudentId}: {Status} ({SchoolYear} {Semester}).",
                studentId, status, schoolYear, semester);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not update Guidance clearance for student {StudentId}.", studentId);
            return false;
        }
    }

    /// <summary>
    /// Convenience overload — resolves the numeric ID from a student number string first.
    /// </summary>
    public async Task<bool> UpsertGuidanceClearanceByStudentNumberAsync(
        string studentIdNumber,
        string schoolYear,
        string semester,
        string status,
        string clearedBy = "Guidance Office",
        string? remarks = null)
    {
        var id = await FindStudentIdByNumberAsync(studentIdNumber);
        if (id is null)
        {
            _logger.LogWarning("Cannot upsert clearance — student number {Number} not found.", studentIdNumber);
            return false;
        }
        return await UpsertGuidanceClearanceAsync(id.Value, schoolYear, semester, status, clearedBy, remarks);
    }

    // -------------------------------------------------------------------------
    // Guidance notes (MySQL)
    // -------------------------------------------------------------------------

    public sealed record GuidanceNoteRow(int Id, int StudentId, string Counselor, DateTime NoteDate,
        string NoteType, string Content, bool IsConfidential);

    /// <summary>Inserts a confidential case note for a student.</summary>
    public async Task<bool> AddNoteAsync(int studentId, string counselor, string noteType,
        string content, bool isConfidential = true)
    {
        try
        {
            await using var db = CreateConnection();
            await db.ExecuteAsync(@"
                INSERT INTO `guidance_notes`
                    (`student_id`, `counselor`, `note_type`, `content`, `is_confidential`)
                VALUES
                    (@studentId, @counselor, @noteType, @content, @isConfidential);",
                new { studentId, counselor, noteType, content, isConfidential });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("AddNoteAsync failed: {Message}", ex.Message);
            return false;
        }
    }

    /// <summary>Returns all case notes for a given student (newest first).</summary>
    public async Task<IReadOnlyList<GuidanceNoteRow>> GetNotesForStudentAsync(int studentId)
    {
        try
        {
            await using var db = CreateConnection();
            var rows = await db.QueryAsync<GuidanceNoteRow>(@"
                SELECT id AS Id, student_id AS StudentId, counselor AS Counselor,
                       note_date AS NoteDate, note_type AS NoteType,
                       content AS Content, is_confidential AS IsConfidential
                FROM   `guidance_notes`
                WHERE  student_id = @studentId
                ORDER BY note_date DESC;",
                new { studentId });
            return rows.AsList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("GetNotesForStudentAsync failed: {Message}", ex.Message);
            return [];
        }
    }
}
