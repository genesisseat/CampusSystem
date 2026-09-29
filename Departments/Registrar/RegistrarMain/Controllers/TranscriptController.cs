using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Dapper;
using MySqlConnector;
using RegistrarMain.Contracts;

namespace RegistrarMain.Controllers;

[Route("api/transcript")]
public sealed class TranscriptController(MySqlConnection db) : RegistrarControllerBase(db)
{
    [HttpGet("mine")]
    [Authorize]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken)
    {
        var studentId = await GetCurrentStudentIdAsync(cancellationToken);
        if (studentId is null) return MissingStudent();

        const string query = @"
            SELECT CONCAT(e.school_year, ' ', e.semester) AS Semester,
                   s.subject_code AS CourseCode,
                   CASE WHEN g.is_inc = 1 THEN 'INC' ELSE CAST(g.grade AS CHAR) END AS Grade
            FROM grades g
            JOIN enrolled_subjects es ON es.id = g.enrolled_subject_id
            JOIN enrollments e ON e.id = es.enrollment_id
            JOIN class_offerings co ON co.id = es.class_offering_id
            JOIN subjects s ON s.id = co.subject_id
            WHERE e.student_id = @studentId
              AND (g.grade IS NOT NULL OR g.is_inc = 1)
            ORDER BY e.school_year, e.semester, s.subject_code";
        var entries = await Db.QueryAsync<TranscriptRow>(
            new CommandDefinition(query, new { studentId }, cancellationToken: cancellationToken));

        var semesters = entries.GroupBy(entry => entry.Semester)
            .Select(group => new TranscriptSemesterDto(
                group.Key,
                group.Select(entry => new TranscriptEntryDto(entry.CourseCode, entry.Grade)).ToList()))
            .ToList();
        return Ok(new TranscriptDto(semesters));
    }

    private sealed record TranscriptRow(string Semester, string CourseCode, string Grade);
}
