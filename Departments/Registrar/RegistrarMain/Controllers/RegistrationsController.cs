using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RegistrarMain.Contracts;
using RegistrarMain.Data;
using RegistrarMain.Models;

namespace RegistrarMain.Controllers;

[Route("api/registrations")]
public sealed class RegistrationsController(RegistrarDbContext db) : RegistrarControllerBase(db)
{
    [HttpGet("mine")]
    [Authorize]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken)
    {
        var student = await GetCurrentStudentAsync(cancellationToken);
        if (student is null) return MissingStudent();

        var enrollments = await Db.Enrollments.AsNoTracking()
            .Where(enrollment => enrollment.StudentId == student.Id)
            .Include(enrollment => enrollment.Course)
            .OrderBy(enrollment => enrollment.Semester)
            .ThenBy(enrollment => enrollment.Course!.Code)
            .Select(enrollment => ToDto(enrollment))
            .ToListAsync(cancellationToken);
        return Ok(enrollments);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(EnrollmentRequestDto request, CancellationToken cancellationToken)
    {
        var student = await GetCurrentStudentAsync(cancellationToken);
        if (student is null) return MissingStudent();
        if (string.IsNullOrWhiteSpace(request.Semester)) return BadRequest("Semester is required.");

        var courseExists = await Db.Courses.AnyAsync(course => course.Id == request.CourseId, cancellationToken);
        if (!courseExists) return NotFound("Course not found.");

        var alreadyEnrolled = await Db.Enrollments.AnyAsync(enrollment =>
            enrollment.StudentId == student.Id && enrollment.CourseId == request.CourseId && enrollment.Semester == request.Semester.Trim(), cancellationToken);
        if (alreadyEnrolled) return Conflict("The student is already enrolled in this course for the semester.");

        var enrollment = new Enrollment
        {
            StudentId = student.Id,
            CourseId = request.CourseId,
            Semester = request.Semester.Trim()
        };
        Db.Enrollments.Add(enrollment);
        await Db.SaveChangesAsync(cancellationToken);
        await Db.Entry(enrollment).Reference(item => item.Course).LoadAsync(cancellationToken);
        return Created($"/api/registrations/{enrollment.Id}", ToDto(enrollment));
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id, [FromQuery] string rowVersion, CancellationToken cancellationToken)
    {
        if (!TryDecodeRowVersion(rowVersion, out var expectedVersion)) return BadRequest("rowVersion must be a valid base64 value.");
        var student = await GetCurrentStudentAsync(cancellationToken);
        if (student is null) return MissingStudent();

        var enrollment = await Db.Enrollments.SingleOrDefaultAsync(item => item.Id == id && item.StudentId == student.Id, cancellationToken);
        if (enrollment is null) return Conflict("The enrollment was already removed or is no longer owned by this student.");

        Db.Entry(enrollment).Property(item => item.RowVersion).OriginalValue = expectedVersion;
        Db.Enrollments.Remove(enrollment);
        try
        {
            await Db.SaveChangesAsync(cancellationToken);
            return NoContent();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("The enrollment changed before it could be dropped. Refresh and try again.");
        }
    }

    private static EnrollmentDto ToDto(Enrollment enrollment) =>
        new(enrollment.Id, enrollment.CourseId, enrollment.Course?.Code ?? "", enrollment.Semester, EncodeRowVersion(enrollment.RowVersion));
}
