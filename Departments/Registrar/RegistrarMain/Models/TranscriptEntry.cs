using System.ComponentModel.DataAnnotations.Schema;
using CampusSystem.Data.Models;

namespace RegistrarMain.Models;

[Table("TranscriptEntries", Schema = "registrar")]
public class TranscriptEntry
{
    public int Id { get; set; }
    public string StudentId { get; set; } = "";
    public Student? Student { get; set; }
    public string Semester { get; set; } = "";
    public int CourseId { get; set; }
    public string Grade { get; set; } = "";
}
