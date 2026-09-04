using RegistrarMain.Contracts;

namespace RegistrarMain.Services;

public interface IPiiMaskingService
{
    Task<string> RedactAsync(string input, RedactionRules rules, CancellationToken cancellationToken);
}
