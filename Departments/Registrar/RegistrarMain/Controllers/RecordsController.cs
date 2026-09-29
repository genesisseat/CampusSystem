using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Dapper;
using MySqlConnector;
using RegistrarMain.Contracts;

namespace RegistrarMain.Controllers;

[Route("api/records")]
public sealed class RecordsController(MySqlConnection db) : RegistrarControllerBase(db)
{
    [HttpGet("mine")]
    [Authorize]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken)
    {
        var studentId = await GetCurrentStudentIdAsync(cancellationToken);
        if (studentId is null) return MissingStudent();

        const string query = @"
            SELECT id AS Id, document_type AS DocumentType,
                   status AS Status, requested_at AS RequestedAt
            FROM document_requests
            WHERE student_id = @studentId AND document_type <> 'Enrollment Verification'
            ORDER BY requested_at DESC";
        var requests = await Db.QueryAsync<RecordsResponseDto>(
            new CommandDefinition(query, new { studentId }, cancellationToken: cancellationToken));
        return Ok(requests.AsList());
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(RecordsRequestDto request, CancellationToken cancellationToken)
    {
        var studentId = await GetCurrentStudentIdAsync(cancellationToken);
        if (studentId is null) return MissingStudent();
        if (string.IsNullOrWhiteSpace(request.DocumentType)) return BadRequest("Document type is required.");

        const string insert = @"
            INSERT INTO document_requests
                (student_id, document_type, purpose, copies, status, requested_at)
            VALUES (@studentId, @documentType, 'Requested through Registrar portal', 1, 'pending', NOW());
            SELECT LAST_INSERT_ID();";
        var id = await Db.ExecuteScalarAsync<int>(new CommandDefinition(insert, new
        {
            studentId,
            documentType = request.DocumentType.Trim()
        }, cancellationToken: cancellationToken));
        var response = new RecordsResponseDto(id, request.DocumentType.Trim(), "pending", DateTime.UtcNow);
        return Created($"/api/records/{id}", response);
    }
}
