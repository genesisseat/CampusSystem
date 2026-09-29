using GuidanceDepartmentMain.Contracts;

namespace GuidanceDepartmentMain.Services;

public interface IStudentRequestService
{
    Task<ServiceResult<StudentRequestResponse>> CreateAsync(int studentId, StudentRequestDto request, string idempotencyKey, CancellationToken cancellationToken);
    Task<ServiceResult<StudentRequestResponse>> GetAsync(int studentId, Guid requestId, CancellationToken cancellationToken);
    Task<ServiceResult<StudentRequestResponse>> UpdateAsync(int studentId, Guid requestId, StudentRequestDto request, byte[] rowVersion, CancellationToken cancellationToken);
    Task<ServiceResult<bool>> DeleteAsync(int studentId, Guid requestId, byte[] rowVersion, CancellationToken cancellationToken);
}