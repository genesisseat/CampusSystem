using StudentPortalMain.Contracts;

namespace StudentPortalMain.Services;

public interface IAuditLogService
{
    Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken);
    Task<IReadOnlyList<AuditEvent>> QueryAsync(string? entity, DateTimeOffset? since, CancellationToken cancellationToken);
}
