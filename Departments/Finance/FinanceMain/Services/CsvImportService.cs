using CsvHelper;
using CsvHelper.Configuration;
using FinanceMain.Contracts;
using System.Globalization;

namespace FinanceMain.Services;

public sealed class CsvImportService : ICsvImportService
{
    private sealed record RosterRow(string StudentId, string Name);
    private sealed class RosterMap : ClassMap<RosterRow> { public RosterMap() { Map(x => x.StudentId).Name("StudentId"); Map(x => x.Name).Name("Name"); } }
    public async Task<ImportValidationResult> ValidateAsync(Stream csv, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(csv, leaveOpen: true); using var parser = new CsvReader(reader, CultureInfo.InvariantCulture); parser.Context.RegisterClassMap<RosterMap>();
        var errors = new List<ImportRowError>(); var count = 0;
        try { await foreach (var row in parser.GetRecordsAsync<RosterRow>(cancellationToken)) { count++; if (string.IsNullOrWhiteSpace(row.StudentId)) errors.Add(new(count, "StudentId", "Required.")); } }
        catch (CsvHelperException ex) { errors.Add(new(0, "schema", ex.Message)); }
        return new ImportValidationResult(errors.Count == 0, count, errors, []);
    }
    public async Task<ServiceResult<int>> CommitAsync(Stream csv, CancellationToken cancellationToken)
    {
        if (csv.CanSeek) csv.Position = 0;
        var validation = await ValidateAsync(csv, cancellationToken); if (!validation.Passed) return ServiceResult<int>.Fail("validation", "Import dry run failed.");
        return ServiceResult<int>.Ok(validation.RowCount);
    }
}
