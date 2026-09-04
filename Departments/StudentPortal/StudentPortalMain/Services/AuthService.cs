using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using StudentPortalMain.Contracts;
using Microsoft.IdentityModel.Tokens;

namespace StudentPortalMain.Services;

public sealed class AuthService(IConfiguration configuration, IRefreshTokenStore refreshTokens) : IAuthService
{
    public async Task<ServiceResult<AuthResponse>> LoginAsync(AuthRequest request, HttpResponse response, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password) || request.Role is not ("Student" or "Counselor")) return ServiceResult<AuthResponse>.Fail("unauthorized", "Invalid credentials.");
        return await IssueAsync(request.UserName, request.Role, response, cancellationToken);
    }

    public async Task<ServiceResult<AuthResponse>> RefreshAsync(string refreshToken, HttpResponse response, CancellationToken cancellationToken)
    {
        if (!await refreshTokens.ConsumeAsync(refreshToken, cancellationToken)) return ServiceResult<AuthResponse>.Fail("unauthorized", "Invalid refresh token.");
        return await IssueAsync("refresh-subject", "Student", response, cancellationToken);
    }

    public Guid? GetStudentId(ClaimsPrincipal principal) => principal.GetStudentId();

    private async Task<ServiceResult<AuthResponse>> IssueAsync(string subject, string role, HttpResponse response, CancellationToken cancellationToken)
    {
        var expires = DateTimeOffset.UtcNow.AddMinutes(20);
        var key = configuration["Jwt:SigningKey"];
        if (string.IsNullOrWhiteSpace(key)) return ServiceResult<AuthResponse>.Fail("configuration", "Jwt:SigningKey is not configured.");
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(claims: [new Claim(ClaimTypes.NameIdentifier, subject), new Claim(ClaimTypes.Role, role)], expires: expires.UtcDateTime, signingCredentials: credentials);
        var refresh = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        await refreshTokens.StoreAsync(refresh, subject, DateTimeOffset.UtcNow.AddDays(7), cancellationToken);
        response.Cookies.Append("access_token", new JwtSecurityTokenHandler().WriteToken(jwt), new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Strict, Secure = true, Expires = expires });
        response.Cookies.Append("refresh_token", refresh, new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Strict, Secure = true, Expires = DateTimeOffset.UtcNow.AddDays(7) });
        return ServiceResult<AuthResponse>.Ok(new AuthResponse(role, expires));
    }
}
