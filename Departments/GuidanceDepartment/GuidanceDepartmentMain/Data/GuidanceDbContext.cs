using CampusSystem.Data.Models;
using GuidanceDepartmentMain.Services;
using Microsoft.EntityFrameworkCore;

namespace GuidanceDepartmentMain.Data;

public sealed class GuidanceDbContext(DbContextOptions<GuidanceDbContext> options) : DbContext(options)
{
    public DbSet<Student> Students => Set<Student>();
    public DbSet<GuidanceRequestRecord> GuidanceRequests => Set<GuidanceRequestRecord>();
    public DbSet<RefreshTokenRecord> RefreshTokens => Set<RefreshTokenRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Student>()
            .ToTable("Students", "dbo")
            .HasKey(student => student.Id);

        modelBuilder.Entity<GuidanceRequestRecord>()
            .ToTable("GuidanceRequests", "guidance")
            .HasKey(r => r.Id);

        modelBuilder.Entity<RefreshTokenRecord>()
            .ToTable("RefreshTokens", "guidance")
            .HasKey(r => r.Token);

        base.OnModelCreating(modelBuilder);
    }
}

public sealed record RefreshTokenRecord(string Token, string Subject, DateTimeOffset ExpiresAt);
