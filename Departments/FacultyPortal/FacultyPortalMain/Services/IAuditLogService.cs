using FacultyPortalMain.Contracts;

namespace FacultyPortalMain.Services;

public interface IAuditLogService
{
    Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken);
    Task<IReadOnlyList<AuditEvent>> QueryAsync(string? entity, DateTimeOffset? since, CancellationToken cancellationToken);
}
