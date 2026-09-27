using Microsoft.EntityFrameworkCore;
using RegistrarMain.Contracts;
using RegistrarMain.Services;

namespace RegistrarMain.Data;

public sealed record RefreshTokenRecord(string Token, string Subject, DateTimeOffset ExpiresAt);

public class GuidanceDbContext(DbContextOptions<GuidanceDbContext> options) : DbContext(options)
{
    public DbSet<GuidanceRequestRecord> GuidanceRequests => Set<GuidanceRequestRecord>();
    public DbSet<RefreshTokenRecord> RefreshTokens => Set<RefreshTokenRecord>();
}
