using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RegistrarMain.Contracts;
using RegistrarMain.Data;
using RegistrarMain.Models;

namespace RegistrarMain.Controllers;

[Route("api/records")]
public sealed class RecordsController(RegistrarDbContext db) : RegistrarControllerBase(db)
{
    [HttpGet("mine")]
    [Authorize]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken)
    {
        var student = await GetCurrentStudentAsync(cancellationToken);
        if (student is null) return MissingStudent();

        var requests = await Db.RecordsRequests.AsNoTracking()
            .Where(request => request.StudentId == student.Id)
            .OrderByDescending(request => request.RequestedAt)
            .Select(request => new RecordsResponseDto(request.Id, request.DocumentType, request.Status, request.RequestedAt))
            .ToListAsync(cancellationToken);
        return Ok(requests);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(RecordsRequestDto request, CancellationToken cancellationToken)
    {
        var student = await GetCurrentStudentAsync(cancellationToken);
        if (student is null) return MissingStudent();
        if (string.IsNullOrWhiteSpace(request.DocumentType)) return BadRequest("Document type is required.");

        var record = new RecordsRequest
        {
            StudentId = student.Id,
            DocumentType = request.DocumentType.Trim(),
            Status = "Pending",
            RequestedAt = DateTime.UtcNow
        };
        Db.RecordsRequests.Add(record);
        await Db.SaveChangesAsync(cancellationToken);
        var response = new RecordsResponseDto(record.Id, record.DocumentType, record.Status, record.RequestedAt);
        return Created($"/api/records/{record.Id}", response);
    }
}
