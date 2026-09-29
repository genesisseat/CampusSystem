using System.Security.Cryptography;
using Dapper;
using GuidanceDepartmentMain.Contracts;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace GuidanceDepartmentMain.Services;

public sealed class MySqlGuidancePersistenceStores(IConfiguration configuration)
    : IGuidanceRequestStore, IRefreshTokenStore
{
    private const string RequestColumns = @"
        SELECT id AS Id,
               student_id AS StudentId,
               subject AS Subject,
               details AS Details,
               safety_valve_text AS SafetyValveText,
               urgency AS Urgency,
               status AS Status,
               assigned_counselor_id AS AssignedCounselorId,
               idempotency_key AS IdempotencyKey,
               row_version AS RowVersion
        FROM guidance_requests";

    private string ConnectionString => configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is required.");

    private MySqlConnection CreateConnection() => new(ConnectionString);

    public async Task<GuidanceRequestRecord?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var db = CreateConnection();
        return await db.QuerySingleOrDefaultAsync<GuidanceRequestRecord>(
            RequestColumns + " WHERE id = @id",
            new { id = id.ToString("D") });
    }

    public async Task<GuidanceRequestRecord?> FindByIdempotencyKeyAsync(
        int studentId,
        string key,
        CancellationToken cancellationToken)
    {
        await using var db = CreateConnection();
        return await db.QuerySingleOrDefaultAsync<GuidanceRequestRecord>(
            RequestColumns + " WHERE student_id = @studentId AND idempotency_key = @key",
            new { studentId, key });
    }

    public async Task<IReadOnlyList<GuidanceRequestRecord>> ListAsync(
        int? studentId,
        TriageFilter filter,
        CancellationToken cancellationToken)
    {
        var sql = RequestColumns + " WHERE 1 = 1";
        var parameters = new DynamicParameters();

        if (studentId.HasValue)
        {
            sql += " AND student_id = @studentId";
            parameters.Add("studentId", studentId.Value);
        }

        if (filter.Status.HasValue)
        {
            sql += " AND status = @status";
            parameters.Add("status", (int)filter.Status.Value);
        }

        if (filter.Urgency.HasValue)
        {
            sql += " AND urgency = @urgency";
            parameters.Add("urgency", (int)filter.Urgency.Value);
        }

        if (filter.AssignedCounselorId.HasValue)
        {
            sql += " AND assigned_counselor_id = @assignedCounselorId";
            parameters.Add("assignedCounselorId", filter.AssignedCounselorId.Value.ToString("D"));
        }

        await using var db = CreateConnection();
        var requests = await db.QueryAsync<GuidanceRequestRecord>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
        return requests.AsList();
    }

    public async Task AddAsync(GuidanceRequestRecord request, CancellationToken cancellationToken)
    {
        await using var db = CreateConnection();
        await db.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO guidance_requests
                (id, student_id, subject, details, safety_valve_text, urgency, status,
                 assigned_counselor_id, idempotency_key, row_version)
            VALUES
                (@id, @studentId, @subject, @details, @safetyValveText, @urgency, @status,
                 @assignedCounselorId, @idempotencyKey, @rowVersion)",
            new
            {
                id = request.Id.ToString("D"),
                studentId = request.StudentId,
                subject = request.Subject,
                details = request.Details,
                safetyValveText = request.SafetyValveText,
                urgency = (int)request.Urgency,
                status = (int)request.Status,
                assignedCounselorId = request.AssignedCounselorId?.ToString("D"),
                idempotencyKey = request.IdempotencyKey,
                rowVersion = NormalizeVersion(request.RowVersion)
            },
            cancellationToken: cancellationToken));
        request.RowVersion = NormalizeVersion(request.RowVersion);
    }

    public async Task SaveAsync(
        GuidanceRequestRecord request,
        byte[] expectedVersion,
        CancellationToken cancellationToken)
    {
        var nextVersion = RandomNumberGenerator.GetBytes(8);
        await using var db = CreateConnection();
        var affected = await db.ExecuteAsync(new CommandDefinition(@"
            UPDATE guidance_requests
            SET subject = @subject,
                details = @details,
                safety_valve_text = @safetyValveText,
                urgency = @urgency,
                status = @status,
                assigned_counselor_id = @assignedCounselorId,
                row_version = @nextVersion
            WHERE id = @id AND row_version = @expectedVersion",
            new
            {
                id = request.Id.ToString("D"),
                subject = request.Subject,
                details = request.Details,
                safetyValveText = request.SafetyValveText,
                urgency = (int)request.Urgency,
                status = (int)request.Status,
                assignedCounselorId = request.AssignedCounselorId?.ToString("D"),
                nextVersion,
                expectedVersion
            },
            cancellationToken: cancellationToken));

        if (affected == 0)
        {
            throw new DbUpdateConcurrencyException();
        }

        request.RowVersion = nextVersion;
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        int studentId,
        byte[] expectedVersion,
        CancellationToken cancellationToken)
    {
        await using var db = CreateConnection();
        var affected = await db.ExecuteAsync(new CommandDefinition(@"
            DELETE FROM guidance_requests
            WHERE id = @id AND student_id = @studentId AND row_version = @expectedVersion",
            new { id = id.ToString("D"), studentId, expectedVersion },
            cancellationToken: cancellationToken));

        if (affected > 0)
        {
            return true;
        }

        var exists = await db.ExecuteScalarAsync<bool>(new CommandDefinition(@"
            SELECT EXISTS(
                SELECT 1 FROM guidance_requests
                WHERE id = @id AND student_id = @studentId)",
            new { id = id.ToString("D"), studentId },
            cancellationToken: cancellationToken));

        if (exists)
        {
            throw new DbUpdateConcurrencyException();
        }

        return false;
    }

    public async Task StoreAsync(
        string token,
        string subject,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        await using var db = CreateConnection();
        await db.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO refresh_tokens (token, subject, expires_at)
            VALUES (@token, @subject, @expiresAt)",
            new { token, subject, expiresAt = expiresAt.UtcDateTime },
            cancellationToken: cancellationToken));
    }

    public async Task<bool> ConsumeAsync(string token, CancellationToken cancellationToken)
    {
        await using var db = CreateConnection();
        var affected = await db.ExecuteAsync(new CommandDefinition(@"
            DELETE FROM refresh_tokens
            WHERE token = @token AND expires_at > UTC_TIMESTAMP(6)",
            new { token },
            cancellationToken: cancellationToken));
        return affected > 0;
    }

    private static byte[] NormalizeVersion(byte[]? version)
    {
        var result = new byte[8];
        if (version is { Length: > 0 })
        {
            Array.Copy(version, result, Math.Min(version.Length, result.Length));
        }

        return result;
    }
}