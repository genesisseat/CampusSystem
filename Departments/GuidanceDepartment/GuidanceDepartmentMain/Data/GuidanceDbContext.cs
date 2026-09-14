using CampusSystem.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace GuidanceDepartmentMain.Data;

public sealed class GuidanceDbContext(DbContextOptions<GuidanceDbContext> options) : DbContext(options)
{
    public DbSet<Student> Students => Set<Student>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Student>()
            .ToTable("Students", "dbo")
            .HasKey(student => student.Id);

        base.OnModelCreating(modelBuilder);
    }
}
