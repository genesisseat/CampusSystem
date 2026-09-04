using GuidanceDepartmentMain.Contracts;

namespace GuidanceDepartmentMain.Services;

public interface IPiiMaskingService
{
    Task<string> RedactAsync(string input, RedactionRules rules, CancellationToken cancellationToken);
}