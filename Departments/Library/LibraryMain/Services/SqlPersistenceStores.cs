using LibraryMain.Contracts;
using LibraryMain.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryMain.Services;

public sealed class SqlGuidanceRequestStore(IDbContextFactory<GuidanceDbContext> dbContextFactory) : IGuidanceRequestStore
{
    public async Task<GuidanceRequestRecord?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.GuidanceRequests.FindAsync([id], cancellationToken);
    }

    public async Task<GuidanceRequestRecord?> FindByIdempotencyKeyAsync(Guid studentId, string key, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.GuidanceRequests.FirstOrDefaultAsync(x => x.StudentId == studentId && x.IdempotencyKey == key, cancellationToken);
    }

    public async Task<IReadOnlyList<GuidanceRequestRecord>> ListAsync(Guid? studentId, TriageFilter filter, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var query = context.GuidanceRequests.AsQueryable();
        if (studentId.HasValue) query = query.Where(x => x.StudentId == studentId.Value);
        if (filter.Status.HasValue) query = query.Where(x => x.Status == filter.Status);
        if (filter.Urgency.HasValue) query = query.Where(x => x.Urgency == filter.Urgency);
        if (filter.AssignedCounselorId.HasValue) query = query.Where(x => x.AssignedCounselorId == filter.AssignedCounselorId);
        return await query.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(GuidanceRequestRecord request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.GuidanceRequests.Add(request);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveAsync(GuidanceRequestRecord request, byte[] expectedVersion, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.GuidanceRequests.Update(request);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new DbUpdateConcurrencyException(); // Simple pass-through for now
        }
    }

    public async Task<bool> DeleteAsync(Guid id, Guid studentId, byte[] expectedVersion, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var request = await context.GuidanceRequests.FindAsync([id], cancellationToken);
        if (request is null || request.StudentId != studentId) return false;

        context.GuidanceRequests.Remove(request);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }
}

public sealed class SqlRefreshTokenStore(IDbContextFactory<GuidanceDbContext> dbContextFactory) : IRefreshTokenStore
{
    public async Task StoreAsync(string token, string subject, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.RefreshTokens.Add(new RefreshTokenRecord(token, subject, expiresAt));
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ConsumeAsync(string token, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var record = await context.RefreshTokens.FindAsync([token], cancellationToken);
        if (record is null) return false;
        context.RefreshTokens.Remove(record);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

