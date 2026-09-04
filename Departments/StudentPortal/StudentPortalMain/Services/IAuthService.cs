using StudentPortalMain.Contracts;

namespace StudentPortalMain.Services;

public interface IAuthService
{
    Task<ServiceResult<AuthResponse>> LoginAsync(AuthRequest request, HttpResponse response, CancellationToken cancellationToken);
    Task<ServiceResult<AuthResponse>> RefreshAsync(string refreshToken, HttpResponse response, CancellationToken cancellationToken);
    Guid? GetStudentId(System.Security.Claims.ClaimsPrincipal principal);
}
