using RegistrarMain.Contracts;
using Microsoft.EntityFrameworkCore;

namespace RegistrarMain.Services;

public sealed class GuidanceRequestRecord
{
    public Guid Id { get; init; }
    public Guid StudentId { get; init; }
    public string Subject { get; set; } = "";
    public string Details { get; set; } = "";
    public string? SafetyValveText { get; set; }
    public RequestUrgency Urgency { get; set; }
    public RequestStatus Status { get; set; }
    public Guid? AssignedCounselorId { get; set; }
    public string? IdempotencyKey { get; init; }
    public byte[] RowVersion { get; set; } = [1];
}

public interface IGuidanceRequestStore
{
    Task<GuidanceRequestRecord?> FindAsync(Guid id, CancellationToken cancellationToken);
    Task<GuidanceRequestRecord?> FindByIdempotencyKeyAsync(Guid studentId, string key, CancellationToken cancellationToken);
    Task<IReadOnlyList<GuidanceRequestRecord>> ListAsync(Guid? studentId, TriageFilter filter, CancellationToken cancellationToken);
    Task AddAsync(GuidanceRequestRecord request, CancellationToken cancellationToken);
    Task SaveAsync(GuidanceRequestRecord request, byte[] expectedVersion, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, Guid studentId, byte[] expectedVersion, CancellationToken cancellationToken);
}

public interface IRefreshTokenStore
{
    Task StoreAsync(string token, string subject, DateTimeOffset expiresAt, CancellationToken cancellationToken);
    Task<bool> ConsumeAsync(string token, CancellationToken cancellationToken);
}

public interface IOutboundMessageTransport
{
    Task SendAsync(NotificationMessage message, CancellationToken cancellationToken);
}

public sealed class UnavailableOutboundMessageTransport : IOutboundMessageTransport
{
    public Task SendAsync(NotificationMessage message, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("No outbound message transport is configured.");
}

public sealed class InMemoryRefreshTokenStore : IRefreshTokenStore
{
    private readonly Dictionary<string, (string Subject, DateTimeOffset Expires)> tokens = [];
    public Task StoreAsync(string token, string subject, DateTimeOffset expiresAt, CancellationToken cancellationToken) { tokens[token] = (subject, expiresAt); return Task.CompletedTask; }
    public Task<bool> ConsumeAsync(string token, CancellationToken cancellationToken) => Task.FromResult(tokens.Remove(token));
}

public sealed class InMemoryGuidanceRequestStore : IGuidanceRequestStore
{
    private readonly object sync = new();
    private readonly List<GuidanceRequestRecord> requests = [];
    public Task<GuidanceRequestRecord?> FindAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(requests.FirstOrDefault(x => x.Id == id));
    public Task<GuidanceRequestRecord?> FindByIdempotencyKeyAsync(Guid studentId, string key, CancellationToken cancellationToken) => Task.FromResult(requests.FirstOrDefault(x => x.StudentId == studentId && x.IdempotencyKey == key));
    public Task<IReadOnlyList<GuidanceRequestRecord>> ListAsync(Guid? studentId, TriageFilter filter, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<GuidanceRequestRecord>>(requests.Where(x => (!studentId.HasValue || x.StudentId == studentId) && (!filter.Status.HasValue || x.Status == filter.Status) && (!filter.Urgency.HasValue || x.Urgency == filter.Urgency) && (!filter.AssignedCounselorId.HasValue || x.AssignedCounselorId == filter.AssignedCounselorId)).ToList());
    public Task AddAsync(GuidanceRequestRecord request, CancellationToken cancellationToken) { lock (sync) requests.Add(request); return Task.CompletedTask; }
    public Task SaveAsync(GuidanceRequestRecord request, byte[] expectedVersion, CancellationToken cancellationToken)
    {
        lock (sync)
        {
            var current = requests.FirstOrDefault(x => x.Id == request.Id);
            if (current is null || !current.RowVersion.SequenceEqual(expectedVersion)) throw new DbUpdateConcurrencyException();
            request.RowVersion = [.. expectedVersion.Select(x => x).Concat(new byte[] { 1 }).Take(8)];
        }
        return Task.CompletedTask;
    }
    public Task<bool> DeleteAsync(Guid id, Guid studentId, byte[] expectedVersion, CancellationToken cancellationToken)
    {
        lock (sync)
        {
            var request = requests.FirstOrDefault(x => x.Id == id && x.StudentId == studentId);
            if (request is null || !request.RowVersion.SequenceEqual(expectedVersion)) throw new DbUpdateConcurrencyException();
            requests.Remove(request); return Task.FromResult(true);
        }
    }
}
