using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Dapper;
using MySqlConnector;
using RegistrarMain.Contracts;

namespace RegistrarMain.Controllers;

[ApiController]
[Route("api/courses")]
public sealed class CoursesController(MySqlConnection db) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<CourseDto>>> List([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var query = @"
            SELECT s.id AS Id,
                   s.subject_code AS Code,
                   s.subject_name AS Title,
                   s.units AS Credits,
                   co.id AS OfferingId,
                   co.section_code AS SectionCode,
                   co.semester AS Semester,
                   co.school_year AS SchoolYear
            FROM subjects s
            JOIN class_offerings co ON co.subject_id = s.id
            WHERE co.status = 'open'";

        var parameters = new DynamicParameters();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query += " AND (s.subject_code LIKE @pattern OR s.subject_name LIKE @pattern)";
            parameters.Add("pattern", $"%{search.Trim()}%");
        }

        query += " ORDER BY s.subject_code, co.school_year, co.semester, co.section_code";
        var courses = await db.QueryAsync<CourseDto>(new CommandDefinition(query, parameters, cancellationToken: cancellationToken));
        return Ok(courses.AsList());
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<ActionResult<CourseDto>> Get(int id, CancellationToken cancellationToken)
    {
        const string query = @"
            SELECT s.id AS Id,
                   s.subject_code AS Code,
                   s.subject_name AS Title,
                   s.units AS Credits,
                   co.id AS OfferingId,
                   co.section_code AS SectionCode,
                   co.semester AS Semester,
                   co.school_year AS SchoolYear
            FROM subjects s
            LEFT JOIN class_offerings co ON co.subject_id = s.id AND co.status = 'open'
            WHERE s.id = @id
            ORDER BY co.school_year DESC, co.semester, co.section_code
            LIMIT 1";
        var course = await db.QuerySingleOrDefaultAsync<CourseDto>(
            new CommandDefinition(query, new { id }, cancellationToken: cancellationToken));
        return course is null ? NotFound() : Ok(course);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CourseDto>> Create(CreateCourseRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Title) || request.Credits <= 0)
            return BadRequest("Code, title, and positive credits are required.");

        const string insert = @"
            INSERT INTO subjects
                (subject_code, subject_name, units, lec_hours, lab_hours,
                 prerequisite_subject_id, curriculum_program, year_level, semester)
            VALUES
                (@code, @title, @credits, @lectureHours, @labHours,
                 @prerequisiteSubjectId, @program, @yearLevel, @semester);
            SELECT LAST_INSERT_ID();";
        var id = await db.ExecuteScalarAsync<int>(new CommandDefinition(insert, new
        {
            code = request.Code.Trim(),
            title = request.Title.Trim(),
            credits = request.Credits,
            lectureHours = request.LectureHours,
            labHours = request.LabHours,
            prerequisiteSubjectId = request.PrerequisiteSubjectId,
            program = request.Program.Trim(),
            yearLevel = request.YearLevel.Trim(),
            semester = request.Semester.Trim()
        }, cancellationToken: cancellationToken));
        var response = new CourseDto(id, request.Code.Trim(), request.Title.Trim(), request.Credits);
        return CreatedAtAction(nameof(Get), new { id }, response);
    }
}
