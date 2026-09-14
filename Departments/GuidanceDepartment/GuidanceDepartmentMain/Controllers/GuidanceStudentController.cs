using CampusSystem.Data.Models;
using GuidanceDepartmentMain.Contracts;
using GuidanceDepartmentMain.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GuidanceDepartmentMain.Controllers;

[ApiController]
[Route("api/guidance")]
public sealed class GuidanceStudentController(IDbContextFactory<GuidanceDbContext> dbContextFactory) : ControllerBase
{
    [HttpGet("students")]
    public async Task<ActionResult<List<StudentSummaryDto>>> GetStudents(CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var students = await context.Students
            .AsNoTracking()
            .OrderBy(x => x.FullName)
            .Select(x => new StudentSummaryDto(
                x.Id,
                x.StudentNumber,
                x.FullName,
                x.Email))
            .ToListAsync(cancellationToken);

        return Ok(students);
    }
}
