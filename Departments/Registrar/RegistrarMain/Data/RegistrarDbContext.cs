using CampusSystem.Data.Models;
using Microsoft.EntityFrameworkCore;
using RegistrarMain.Models;

namespace RegistrarMain.Data;

public class RegistrarDbContext(DbContextOptions<RegistrarDbContext> options) : DbContext(options)
{
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<TranscriptEntry> TranscriptEntries => Set<TranscriptEntry>();
    public DbSet<VerificationRequest> VerificationRequests => Set<VerificationRequest>();
    public DbSet<RecordsRequest> RecordsRequests => Set<RecordsRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Student>()
            .ToTable("Students", "dbo", table => table.ExcludeFromMigrations());

        modelBuilder.Entity<Enrollment>()
            .Property(enrollment => enrollment.RowVersion)
            .IsRowVersion();

        modelBuilder.Entity<Enrollment>()
            .HasOne(enrollment => enrollment.Student)
            .WithMany()
            .HasForeignKey(enrollment => enrollment.StudentId)
            .HasPrincipalKey(student => student.Id)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TranscriptEntry>()
            .HasOne(entry => entry.Student)
            .WithMany()
            .HasForeignKey(entry => entry.StudentId)
            .HasPrincipalKey(student => student.Id)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<VerificationRequest>()
            .HasOne(request => request.Student)
            .WithMany()
            .HasForeignKey(request => request.StudentId)
            .HasPrincipalKey(student => student.Id)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RecordsRequest>()
            .HasOne(request => request.Student)
            .WithMany()
            .HasForeignKey(request => request.StudentId)
            .HasPrincipalKey(student => student.Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
