using System.ComponentModel.DataAnnotations.Schema;

namespace CampusSystem.Data.Models;

[Table("Students", Schema = "dbo")]
public class Student
{
    public string Id { get; set; } = "";
    public string StudentNumber { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
}
