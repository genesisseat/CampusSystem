using FacultyPortalMain.Contracts;

namespace FacultyPortalMain.Services;

public interface IPiiMaskingService
{
    Task<string> RedactAsync(string input, RedactionRules rules, CancellationToken cancellationToken);
}
