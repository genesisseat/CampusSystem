using System.Security.Claims;
using CampusSystem.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RegistrarMain.Data;

namespace RegistrarMain.Controllers;

[ApiController]
public abstract class RegistrarControllerBase(RegistrarDbContext db) : ControllerBase
{
    protected RegistrarDbContext Db { get; } = db;

    protected async Task<Student?> GetCurrentStudentAsync(CancellationToken cancellationToken)
    {
        var studentId = User.FindFirstValue("StudentId");
        return string.IsNullOrWhiteSpace(studentId)
            ? null
            : await Db.Set<Student>().SingleOrDefaultAsync(student => student.Id == studentId, cancellationToken);
    }

    protected IActionResult MissingStudent() => Problem(
        statusCode: StatusCodes.Status403Forbidden,
        title: "Student identity is not available",
        detail: "The authenticated student claim does not resolve to a registered student.");

    protected static string EncodeRowVersion(byte[] rowVersion) => Convert.ToBase64String(rowVersion);

    protected static bool TryDecodeRowVersion(string encoded, out byte[] rowVersion)
    {
        try
        {
            rowVersion = Convert.FromBase64String(encoded);
            return rowVersion.Length > 0;
        }
        catch (FormatException)
        {
            rowVersion = [];
            return false;
        }
    }
}
