using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Dapper;
using MySqlConnector;
using RegistrarMain.Contracts;

namespace RegistrarMain.Controllers;

[ApiController]
[Route("api/registrations")]
public sealed class RegistrationsController(MySqlConnection db) : RegistrarControllerBase(db)
{
    [HttpGet("mine")]
    [Authorize]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken)
    {
        var studentId = await GetCurrentStudentIdAsync(cancellationToken);
        if (studentId is null) return Forbid();

        const string query = @"
            SELECT es.id AS Id,
                   s.id AS CourseId,
                   s.subject_code AS CourseCode,
                   CONCAT(e.school_year, ' ', e.semester) AS Semester
            FROM enrolled_subjects es
            JOIN enrollments e ON e.id = es.enrollment_id
            JOIN class_offerings co ON co.id = es.class_offering_id
            JOIN subjects s ON s.id = co.subject_id
            WHERE e.student_id = @studentId AND es.status = 'enrolled'
            ORDER BY e.school_year, e.semester, s.subject_code";
        var enrollments = await Db.QueryAsync<EnrollmentDto>(
            new CommandDefinition(query, new { studentId }, cancellationToken: cancellationToken));
        return Ok(enrollments.AsList());
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(EnrollmentRequestDto request, CancellationToken cancellationToken)
    {
        var studentId = await GetCurrentStudentIdAsync(cancellationToken);
        if (studentId is null) return Forbid();
        if (request.OfferingId <= 0) return BadRequest("A valid offeringId is required.");

        await Db.OpenAsync(cancellationToken);
        await using var transaction = await Db.BeginTransactionAsync(cancellationToken);
        var offering = await Db.QuerySingleOrDefaultAsync<OfferingRow>(new CommandDefinition(@"
            SELECT co.id AS Id, co.subject_id AS SubjectId, co.school_year AS SchoolYear,
                   co.semester AS Semester, co.capacity AS Capacity,
                   co.slots_taken AS SlotsTaken, co.status AS Status,
                   s.subject_code AS SubjectCode, s.units AS Units
            FROM class_offerings co
            JOIN subjects s ON s.id = co.subject_id
            WHERE co.id = @offeringId
            FOR UPDATE",
            new { request.OfferingId }, transaction, cancellationToken: cancellationToken));
        if (offering is null) return NotFound("Class offering not found.");
        if (offering.Status != "open" || offering.SlotsTaken >= offering.Capacity)
            return Conflict("The class offering is closed or full.");

        var duplicate = await Db.ExecuteScalarAsync<int>(new CommandDefinition(@"
            SELECT COUNT(*)
            FROM enrolled_subjects es
            JOIN enrollments e ON e.id = es.enrollment_id
            WHERE e.student_id = @studentId
              AND e.school_year = @schoolYear
              AND e.semester = @semester
              AND es.class_offering_id = @offeringId
              AND es.status = 'enrolled'",
            new
            {
                studentId,
                offering.SchoolYear,
                offering.Semester,
                request.OfferingId
            }, transaction, cancellationToken: cancellationToken));
        if (duplicate > 0) return Conflict("The student is already enrolled in this offering.");

        var enrollmentId = await Db.QuerySingleOrDefaultAsync<int?>(new CommandDefinition(@"
            SELECT id FROM enrollments
            WHERE student_id = @studentId AND school_year = @schoolYear AND semester = @semester
            LIMIT 1 FOR UPDATE",
            new { studentId, offering.SchoolYear, offering.Semester },
            transaction, cancellationToken: cancellationToken));

        if (enrollmentId is null)
        {
            await Db.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO enrollments
                    (student_id, school_year, semester, enrollment_type, status, total_units)
                VALUES (@studentId, @schoolYear, @semester, 'regular', 'pending', @units)",
                new { studentId, offering.SchoolYear, offering.Semester, units = offering.Units },
                transaction, cancellationToken: cancellationToken));
            enrollmentId = await Db.ExecuteScalarAsync<int>(
                new CommandDefinition("SELECT LAST_INSERT_ID()", transaction: transaction, cancellationToken: cancellationToken));
        }
        else
        {
            await Db.ExecuteAsync(new CommandDefinition(@"
                UPDATE enrollments SET total_units = total_units + @units WHERE id = @enrollmentId",
                new { units = offering.Units, enrollmentId }, transaction, cancellationToken: cancellationToken));
        }

        var enrolledSubjectId = await Db.ExecuteScalarAsync<int>(new CommandDefinition(@"
            INSERT INTO enrolled_subjects (enrollment_id, class_offering_id, status)
            VALUES (@enrollmentId, @offeringId, 'enrolled');
            SELECT LAST_INSERT_ID();",
            new { enrollmentId, offeringId = request.OfferingId }, transaction,
            cancellationToken: cancellationToken));
        await Db.ExecuteAsync(new CommandDefinition(@"
            UPDATE class_offerings SET slots_taken = slots_taken + 1 WHERE id = @offeringId",
            new { offeringId = request.OfferingId }, transaction, cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);
        var response = new EnrollmentDto(enrolledSubjectId, offering.SubjectId, offering.SubjectCode,
            $"{offering.SchoolYear} {offering.Semester}");
        return Created("/api/registrations/mine", response);
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var studentId = await GetCurrentStudentIdAsync(cancellationToken);
        if (studentId is null) return Forbid();

        await Db.OpenAsync(cancellationToken);
        await using var transaction = await Db.BeginTransactionAsync(cancellationToken);
        var dropped = await Db.ExecuteAsync(new CommandDefinition(@"
            UPDATE enrolled_subjects es
            JOIN enrollments e ON e.id = es.enrollment_id
            JOIN class_offerings co ON co.id = es.class_offering_id
            SET es.status = 'dropped',
                co.slots_taken = GREATEST(co.slots_taken - 1, 0)
            WHERE es.id = @id AND e.student_id = @studentId AND es.status = 'enrolled'",
            new { id, studentId }, transaction, cancellationToken: cancellationToken));
        if (dropped == 0) return NotFound();

        await transaction.CommitAsync(cancellationToken);
        return NoContent();
    }

    private sealed record OfferingRow(
        int Id, int SubjectId, string SchoolYear, string Semester, int Capacity,
        int SlotsTaken, string Status, string SubjectCode, int Units);
}
