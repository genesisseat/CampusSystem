using GuidanceDepartmentMain.Contracts;
using GuidanceDepartmentMain.Services;
using Microsoft.AspNetCore.Mvc;

namespace GuidanceDepartmentMain.Controllers;

[ApiController]
[Route("api/guidance")]
public sealed class GuidanceStudentController(
    IGuidanceStudentDirectory guidanceDb,
    ICounselorTriageService counselorTriageService) : ControllerBase
{
    /// <summary>
    /// Returns the list of active enrolled students from the shared MySQL <c>user</c> table.
    /// Replaces the old EF Core SQL Server query that returned empty results in production.
    /// </summary>
    [HttpGet("students")]
    public async Task<ActionResult<List<StudentSummaryDto>>> GetStudents(CancellationToken cancellationToken)
    {
        var students = await guidanceDb.GetStudentsAsync();

        var result = students
            .Select(s => new StudentSummaryDto(s.Id, s.StudentIdNumber, s.Name, s.Email))
            .ToList();

        return Ok(result);
    }

    [HttpPost("/api/v1/incoming-referral")]
    public async Task<ActionResult<StudentRequestResponse>> IncomingReferral(
        [FromBody] IncomingReferralDto referral, CancellationToken cancellationToken)
    {
        var result = await counselorTriageService.ProcessIncomingReferralAsync(referral, cancellationToken);
        return !result.Succeeded ? BadRequest(result) : Ok(result.Value);
    }
}
