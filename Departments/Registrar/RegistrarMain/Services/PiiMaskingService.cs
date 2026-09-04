using System.Text.RegularExpressions;
using RegistrarMain.Contracts;

namespace RegistrarMain.Services;

public sealed partial class PiiMaskingService : IPiiMaskingService
{
    public Task<string> RedactAsync(string input, RedactionRules rules, CancellationToken cancellationToken)
    {
        var output = input;
        if (rules.Emails) output = Email().Replace(output, "[REDACTED-EMAIL]");
        if (rules.PhoneNumbers) output = Phone().Replace(output, "[REDACTED-PHONE]");
        if (rules.SsnLikeValues) output = Ssn().Replace(output, "[REDACTED-ID]");
        return Task.FromResult(output);
    }
    [GeneratedRegex(@"\b[\w.%+-]+@[\w.-]+\.[A-Za-z]{2,}\b")] private static partial Regex Email();
    [GeneratedRegex(@"\b(?:\+?1[-. ]?)?\(?\d{3}\)?[-. ]\d{3}[-. ]\d{4}\b")] private static partial Regex Phone();
    [GeneratedRegex(@"\b\d{3}[- ]\d{2}[- ]\d{4}\b")] private static partial Regex Ssn();
}
