using FacultyPortalMain.Contracts;

namespace FacultyPortalMain.Services;

public interface ICsvImportService
{
    Task<ImportValidationResult> ValidateAsync(Stream csv, CancellationToken cancellationToken);
    Task<ServiceResult<int>> CommitAsync(Stream csv, CancellationToken cancellationToken);
}
