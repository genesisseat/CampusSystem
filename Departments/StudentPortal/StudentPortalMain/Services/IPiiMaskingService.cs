using StudentPortalMain.Contracts;

namespace StudentPortalMain.Services;

public interface IPiiMaskingService
{
    Task<string> RedactAsync(string input, RedactionRules rules, CancellationToken cancellationToken);
}
