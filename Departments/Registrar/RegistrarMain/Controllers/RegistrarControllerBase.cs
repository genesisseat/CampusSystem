using System.Security.Claims;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace RegistrarMain.Controllers;

[ApiController]
public abstract class RegistrarControllerBase(MySqlConnection db) : ControllerBase
{
    protected MySqlConnection Db { get; } = db;

    protected async Task<int?> GetCurrentStudentIdAsync(CancellationToken cancellationToken)
    {
        var studentKey = User.FindFirstValue("StudentId");
        if (string.IsNullOrWhiteSpace(studentKey))
            return null;

        int? numericId = int.TryParse(studentKey, out var parsedId) && parsedId > 0 ? parsedId : null;
        return await Db.QuerySingleOrDefaultAsync<int?>(new CommandDefinition(@"
            SELECT id
            FROM `user`
            WHERE role = 'student'
              AND ((@numericId IS NOT NULL AND id = @numericId) OR student_id_number = @studentKey)
            LIMIT 1",
            new { numericId, studentKey }, cancellationToken: cancellationToken));
    }

    protected IActionResult MissingStudent() => Problem(
        statusCode: StatusCodes.Status403Forbidden,
        title: "Student identity is not available",
        detail: "The authenticated student claim does not resolve to a registered student.");

}
