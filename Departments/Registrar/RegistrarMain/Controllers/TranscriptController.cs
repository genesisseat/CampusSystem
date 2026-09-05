using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RegistrarMain.Contracts;
using RegistrarMain.Data;

namespace RegistrarMain.Controllers;

[Route("api/transcript")]
public sealed class TranscriptController(RegistrarDbContext db) : RegistrarControllerBase(db)
{
    [HttpGet("mine")]
    [Authorize]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken)
    {
        var student = await GetCurrentStudentAsync(cancellationToken);
        if (student is null) return MissingStudent();

        var entries = await Db.TranscriptEntries.AsNoTracking()
            .Where(entry => entry.StudentId == student.Id)
            .Include(entry => entry.Student)
            .Join(Db.Courses, entry => entry.CourseId, course => course.Id, (entry, course) => new
            {
                entry.Semester,
                CourseCode = course.Code,
                entry.Grade
            })
            .OrderBy(entry => entry.Semester)
            .ThenBy(entry => entry.CourseCode)
            .ToListAsync(cancellationToken);

        var semesters = entries.GroupBy(entry => entry.Semester)
            .Select(group => new TranscriptSemesterDto(
                group.Key,
                group.Select(entry => new TranscriptEntryDto(entry.CourseCode, entry.Grade)).ToList()))
            .ToList();
        return Ok(new TranscriptDto(semesters));
    }
}
