using System.ComponentModel.DataAnnotations.Schema;
using CampusSystem.Data.Models;

namespace RegistrarMain.Models;

[Table("Enrollments", Schema = "registrar")]
public class Enrollment
{
    public int Id { get; set; }
    public string StudentId { get; set; } = "";
    public Student? Student { get; set; }
    public int CourseId { get; set; }
    public Course? Course { get; set; }
    public string Semester { get; set; } = "";
    public byte[] RowVersion { get; set; } = null!;
}
