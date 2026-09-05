using System.ComponentModel.DataAnnotations.Schema;
using CampusSystem.Data.Models;

namespace RegistrarMain.Models;

[Table("VerificationRequests", Schema = "registrar")]
public class VerificationRequest
{
    public int Id { get; set; }
    public string StudentId { get; set; } = "";
    public Student? Student { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime RequestedAt { get; set; }
}
