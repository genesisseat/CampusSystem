using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RegistrarMain.Contracts;
using RegistrarMain.Data;
using RegistrarMain.Models;

namespace RegistrarMain.Controllers;

[Route("api/verifications")]
public sealed class VerificationsController(RegistrarDbContext db) : RegistrarControllerBase(db)
{
    [HttpGet("mine")]
    [Authorize]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken)
    {
        var student = await GetCurrentStudentAsync(cancellationToken);
        if (student is null) return MissingStudent();

        var requests = await Db.VerificationRequests.AsNoTracking()
            .Where(request => request.StudentId == student.Id)
            .OrderByDescending(request => request.RequestedAt)
            .Select(request => new VerificationResponseDto(request.Id, request.Status, request.RequestedAt))
            .ToListAsync(cancellationToken);
        return Ok(requests);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var student = await GetCurrentStudentAsync(cancellationToken);
        if (student is null) return MissingStudent();

        var request = new VerificationRequest
        {
            StudentId = student.Id,
            Status = "Pending",
            RequestedAt = DateTime.UtcNow
        };
        Db.VerificationRequests.Add(request);
        await Db.SaveChangesAsync(cancellationToken);
        var response = new VerificationResponseDto(request.Id, request.Status, request.RequestedAt);
        return Created($"/api/verifications/{request.Id}", response);
    }
}
