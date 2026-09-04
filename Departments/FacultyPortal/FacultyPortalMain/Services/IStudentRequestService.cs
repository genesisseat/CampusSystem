using FacultyPortalMain.Contracts;

namespace FacultyPortalMain.Services;

public interface IStudentRequestService
{
    Task<ServiceResult<StudentRequestResponse>> CreateAsync(Guid studentId, StudentRequestDto request, string idempotencyKey, CancellationToken cancellationToken);
    Task<ServiceResult<StudentRequestResponse>> GetAsync(Guid studentId, Guid requestId, CancellationToken cancellationToken);
    Task<ServiceResult<StudentRequestResponse>> UpdateAsync(Guid studentId, Guid requestId, StudentRequestDto request, byte[] rowVersion, CancellationToken cancellationToken);
    Task<ServiceResult<bool>> DeleteAsync(Guid studentId, Guid requestId, byte[] rowVersion, CancellationToken cancellationToken);
}
