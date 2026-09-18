using CampusSystem.Data.Models;
using GuidanceDepartmentMain.Contracts;
using GuidanceDepartmentMain.Data;
using GuidanceDepartmentMain.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GuidanceDepartmentMain.Controllers;

[ApiController]
[Route("api/guidance")]
public sealed class GuidanceStudentController(IDbContextFactory<GuidanceDbContext> dbContextFactory, ICounselorTriageService counselorTriageService) : ControllerBase
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

    [HttpPost("/api/v1/incoming-referral")]
    public async Task<ActionResult<StudentRequestResponse>> IncomingReferral([FromBody] IncomingReferralDto referral, CancellationToken cancellationToken)
    {
        var result = await counselorTriageService.ProcessIncomingReferralAsync(referral, cancellationToken);
        return !result.Succeeded ? BadRequest(result) : Ok(result.Value);
    }
}
