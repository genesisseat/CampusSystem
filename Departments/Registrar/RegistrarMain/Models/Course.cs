using System.ComponentModel.DataAnnotations.Schema;

namespace RegistrarMain.Models;

[Table("Courses", Schema = "registrar")]
public class Course
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Title { get; set; } = "";
    public int Credits { get; set; }
}
