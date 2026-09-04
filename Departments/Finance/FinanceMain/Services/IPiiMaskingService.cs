using FinanceMain.Contracts;

namespace FinanceMain.Services;

public interface IPiiMaskingService
{
    Task<string> RedactAsync(string input, RedactionRules rules, CancellationToken cancellationToken);
}
