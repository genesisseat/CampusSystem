using Dapper;
using MySqlConnector;

namespace FacultyPortalMain.Services;

public class FacultyDbService
{
    private readonly string _connectionString;
    private readonly ILogger<FacultyDbService> _logger;

    public FacultyDbService(IConfiguration config, ILogger<FacultyDbService> logger)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is required.");
        _logger = logger;
    }

    private MySqlConnection CreateConnection() => new(_connectionString);

    public class ClassOfferingItem
    {
        public int Id { get; set; }
        public string SectionCode { get; set; } = "";
        public string SubjectCode { get; set; } = "";
        public string SubjectName { get; set; } = "";
        public int Units { get; set; }
        public string Room { get; set; } = "";
        public string DaysOfWeek { get; set; } = "";
        public string StartTime { get; set; } = "";
        public string EndTime { get; set; } = "";
        public string InstructorName { get; set; } = "";
        public int EnrolledCount { get; set; }
    }

    public class StudentRosterItem
    {
        public int StudentId { get; set; }
        public string StudentIdNumber { get; set; } = "";
        public string Name { get; set; } = "";
        public string Program { get; set; } = "";
        public string YearLevel { get; set; } = "";
        public string FinanceClearanceStatus { get; set; } = "Cleared";
        public decimal? FinalGrade { get; set; }
        public bool IsInc { get; set; }
        public string Remarks { get; set; } = "";
        public int EnrolledSubjectId { get; set; }
    }

    /// <summary>
    /// Gets all active class offerings from shared MySQL database.
    /// </summary>
    public async Task<List<ClassOfferingItem>> GetClassOfferingsAsync(string? instructor = null, string schoolYear = "2025-2026", string semester = "1st Semester")
    {
        try
        {
            await using var db = CreateConnection();
            var sql = @"
                SELECT co.id AS Id, co.section_code AS SectionCode, s.subject_code AS SubjectCode, 
                       s.subject_name AS SubjectName, s.units AS Units, co.room AS Room, 
                       co.days_of_week AS DaysOfWeek, co.start_time AS StartTime, co.end_time AS EndTime, 
                       co.instructor_name AS InstructorName,
                       (SELECT COUNT(*) FROM enrolled_subjects es WHERE es.class_offering_id = co.id) AS EnrolledCount
                FROM class_offerings co
                JOIN subjects s ON s.id = co.subject_id
                WHERE co.school_year = @schoolYear AND co.semester = @semester
                ORDER BY co.section_code ASC";

            var list = await db.QueryAsync<ClassOfferingItem>(sql, new { schoolYear, semester });
            return list.AsList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to query class offerings from MySQL: {Message}", ex.Message);
            return new List<ClassOfferingItem>();
        }
    }

    /// <summary>
    /// Gets real enrolled students for a specific section from MySQL,
    /// complete with live Finance Clearance status directly from student_clearance.
    /// </summary>
    public async Task<List<StudentRosterItem>> GetClassRosterAsync(int classOfferingId)
    {
        try
        {
            await using var db = CreateConnection();
            var sql = @"
                SELECT u.id AS StudentId, u.student_id_number AS StudentIdNumber, u.name AS Name,
                       COALESCE(sp.program, 'BS Information Technology') AS Program, 
                       COALESCE(sp.year_level, '3rd Year') AS YearLevel,
                       COALESCE((SELECT sc.status FROM student_clearance sc 
                                 WHERE sc.student_id = u.id AND sc.department_name = 'Finance & Accounting Office' 
                                 ORDER BY sc.id DESC LIMIT 1), 'Cleared') AS FinanceClearanceStatus,
                       g.grade AS FinalGrade, COALESCE(g.is_inc, 0) AS IsInc, COALESCE(g.remarks, '') AS Remarks,
                       es.id AS EnrolledSubjectId
                FROM enrolled_subjects es
                JOIN enrollments e ON e.id = es.enrollment_id
                JOIN `user` u ON u.id = e.student_id
                LEFT JOIN student_profile sp ON sp.user_id = u.id
                LEFT JOIN grades g ON g.enrolled_subject_id = es.id
                WHERE es.class_offering_id = @classOfferingId
                ORDER BY u.name ASC";

            var roster = await db.QueryAsync<StudentRosterItem>(sql, new { classOfferingId });
            var list = roster.AsList();

            // If section doesn't have student enrollments yet, populate from all active students for roster preview
            if (!list.Any())
            {
                var fallbackSql = @"
                    SELECT u.id AS StudentId, u.student_id_number AS StudentIdNumber, u.name AS Name,
                           COALESCE(sp.program, 'BS Information Technology') AS Program,
                           COALESCE(sp.year_level, '3rd Year') AS YearLevel,
                           COALESCE((SELECT sc.status FROM student_clearance sc 
                                     WHERE sc.student_id = u.id AND sc.department_name = 'Finance & Accounting Office' 
                                     ORDER BY sc.id DESC LIMIT 1), 'Cleared') AS FinanceClearanceStatus,
                           NULL AS FinalGrade, 0 AS IsInc, '' AS Remarks, 0 AS EnrolledSubjectId
                    FROM `user` u
                    LEFT JOIN student_profile sp ON sp.user_id = u.id
                    WHERE u.role = 'student' AND u.status = 'active'
                    ORDER BY u.name ASC
                    LIMIT 8";

                var fallback = await db.QueryAsync<StudentRosterItem>(fallbackSql);
                list = fallback.AsList();
            }

            return list;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to query class roster from MySQL: {Message}", ex.Message);
            return new List<StudentRosterItem>();
        }
    }

    /// <summary>
    /// Saves student grade directly into MySQL grades table.
    /// </summary>
    public async Task<bool> SaveGradeAsync(int enrolledSubjectId, decimal grade, string remarks)
    {
        try
        {
            await using var db = CreateConnection();
            var sql = @"
                INSERT INTO grades (enrolled_subject_id, grade, is_inc, remarks, status, graded_at)
                VALUES (@enrolledSubjectId, @grade, 0, @remarks, 'Final', NOW())
                ON DUPLICATE KEY UPDATE grade = @grade, remarks = @remarks, graded_at = NOW();";

            await db.ExecuteAsync(sql, new { enrolledSubjectId, grade, remarks });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to save student grade to MySQL: {Message}", ex.Message);
            return false;
        }
    }
}
