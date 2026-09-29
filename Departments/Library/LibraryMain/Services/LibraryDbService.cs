using Dapper;
using MySqlConnector;

namespace LibraryMain.Services;

/// <summary>
/// Provides MySQL access to the shared campus database (mydb) for the Library Department.
/// Reads enrolled students from the shared <c>user</c> table and writes clearance decisions
/// to <c>student_clearance</c> so StudentPortal and Registrar reflect them in real-time.
/// </summary>
public sealed class LibraryDbService
{
    private readonly string _connectionString;
    private readonly ILogger<LibraryDbService> _logger;

    public LibraryDbService(IConfiguration config, ILogger<LibraryDbService> logger)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")
            ?? "Server=100.98.41.69;Port=3306;Database=mydb;Uid=myuser;Pwd=strongpassword;";
        _logger = logger;
    }

    private MySqlConnection CreateConnection() => new(_connectionString);

    // -------------------------------------------------------------------------
    // Schema bootstrap
    // -------------------------------------------------------------------------

    /// <summary>
    /// Ensures Library-specific MySQL tables exist (called once on startup).
    /// </summary>
    public async Task EnsureSchemaAsync()
    {
        try
        {
            await using var db = CreateConnection();
            await db.OpenAsync();

            // Library accounts (borrowed books, fines, etc.)
            await db.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS `library_accounts` (
                    `id`            INT AUTO_INCREMENT PRIMARY KEY,
                    `student_id`    INT NOT NULL,
                    `has_overdue`   TINYINT(1) NOT NULL DEFAULT 0,
                    `fine_amount`   DECIMAL(10,2) NOT NULL DEFAULT 0.00,
                    `fine_paid`     TINYINT(1) NOT NULL DEFAULT 0,
                    `updated_at`    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                    UNIQUE KEY `uniq_la_student` (`student_id`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

            // Ensure the shared student_clearance table exists (safe no-op if Finance already created it)
            await db.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS `student_clearance` (
                    `id`              INT AUTO_INCREMENT PRIMARY KEY,
                    `student_id`      INT NOT NULL,
                    `department_name` VARCHAR(100) NOT NULL,
                    `school_year`     VARCHAR(50) NOT NULL,
                    `semester`        VARCHAR(50) NOT NULL,
                    `status`          VARCHAR(50) NOT NULL DEFAULT 'Cleared',
                    `cleared_by`      VARCHAR(150) NOT NULL DEFAULT 'Library Office',
                    `cleared_at`      DATETIME NULL,
                    `remarks`         TEXT NULL,
                    UNIQUE KEY `uniq_stud_dept` (`student_id`, `department_name`, `school_year`, `semester`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

            _logger.LogInformation("Library MySQL schema verified successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Could not verify Library MySQL schema: {Message}. Running in resilient mode.", ex.Message);
        }
    }

    // -------------------------------------------------------------------------
    // Student queries (from shared `user` table)
    // -------------------------------------------------------------------------

    public sealed record StudentRow(int Id, string StudentIdNumber, string Name, string Email);

    /// <summary>
    /// Returns all active enrolled students from the shared MySQL <c>user</c> table.
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
            _logger.LogWarning("GetStudentsAsync failed: {Message}", ex.Message);
            return [];
        }
    }

    /// <summary>Resolves numeric MySQL user.id from a student ID number string.</summary>
    public async Task<int?> FindStudentIdByNumberAsync(string studentIdNumber)
    {
        try
        {
            await using var db = CreateConnection();
            return await db.QueryFirstOrDefaultAsync<int?>(@"
                SELECT id FROM `user`
                WHERE  student_id_number = @studentIdNumber
                  AND  role = 'student'
                LIMIT  1;",
                new { studentIdNumber });
        }
        catch (Exception ex)
        {
            _logger.LogWarning("FindStudentIdByNumberAsync failed: {Message}", ex.Message);
            return null;
        }
    }

    // -------------------------------------------------------------------------
    // Clearance writes (to shared `student_clearance` table)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Upserts a University Library Department clearance row into the shared MySQL
    /// <c>student_clearance</c> table so StudentPortal and Registrar see it immediately.
    /// </summary>
    /// <param name="studentId">Numeric MySQL <c>user.id</c>.</param>
    /// <param name="schoolYear">e.g. "2026-2027"</param>
    /// <param name="semester">e.g. "1st Semester"</param>
    /// <param name="status">"Cleared" | "Pending" | "Hold"</param>
    /// <param name="clearedBy">Name of the librarian issuing the clearance.</param>
    /// <param name="remarks">Optional remarks visible in StudentPortal.</param>
    public async Task<bool> UpsertLibraryClearanceAsync(
        int studentId,
        string schoolYear,
        string semester,
        string status,
        string clearedBy = "Library Office",
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
                    (@studentId, 'University Library Department', @schoolYear, @semester,
                     @status, @clearedBy, NOW(), @remarks)
                ON DUPLICATE KEY UPDATE
                    `status`     = @status,
                    `cleared_by` = @clearedBy,
                    `cleared_at` = NOW(),
                    `remarks`    = @remarks;",
                new { studentId, schoolYear, semester, status, clearedBy, remarks });

            _logger.LogInformation(
                "Library clearance upserted for student {StudentId}: {Status} ({SchoolYear} {Semester}).",
                studentId, status, schoolYear, semester);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("UpsertLibraryClearanceAsync failed: {Message}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Convenience overload — resolves the numeric ID from a student number string first.
    /// </summary>
    public async Task<bool> UpsertLibraryClearanceByStudentNumberAsync(
        string studentIdNumber,
        string schoolYear,
        string semester,
        string status,
        string clearedBy = "Library Office",
        string? remarks = null)
    {
        var id = await FindStudentIdByNumberAsync(studentIdNumber);
        if (id is null)
        {
            _logger.LogWarning("Cannot upsert library clearance — student number {Number} not found.", studentIdNumber);
            return false;
        }
        return await UpsertLibraryClearanceAsync(id.Value, schoolYear, semester, status, clearedBy, remarks);
    }

    // -------------------------------------------------------------------------
    // Library account management
    // -------------------------------------------------------------------------

    /// <summary>
    /// Updates a student's library account status (overdue books, unpaid fines).
    /// A student with unpaid fines should be set to Hold status in clearance.
    /// </summary>
    public async Task<bool> UpdateLibraryAccountAsync(int studentId, bool hasOverdue, decimal fineAmount, bool finePaid)
    {
        try
        {
            await using var db = CreateConnection();
            await db.ExecuteAsync(@"
                INSERT INTO `library_accounts`
                    (`student_id`, `has_overdue`, `fine_amount`, `fine_paid`)
                VALUES
                    (@studentId, @hasOverdue, @fineAmount, @finePaid)
                ON DUPLICATE KEY UPDATE
                    `has_overdue` = @hasOverdue,
                    `fine_amount` = @fineAmount,
                    `fine_paid`   = @finePaid,
                    `updated_at`  = NOW();",
                new { studentId, hasOverdue, fineAmount, finePaid });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("UpdateLibraryAccountAsync failed: {Message}", ex.Message);
            return false;
        }
    }
}
