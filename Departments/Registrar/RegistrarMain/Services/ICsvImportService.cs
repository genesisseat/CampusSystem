using RegistrarMain.Contracts;

namespace RegistrarMain.Services;

public interface ICsvImportService
{
    Task<ImportValidationResult> ValidateAsync(Stream csv, CancellationToken cancellationToken);
    Task<ServiceResult<int>> CommitAsync(Stream csv, CancellationToken cancellationToken);
}
