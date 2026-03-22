using HansOS.Api.Models.Auth;

namespace HansOS.Api.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, HttpContext httpContext, CancellationToken ct = default);
    Task<string> RefreshTokenAsync(HttpContext httpContext, CancellationToken ct = default);
    Task LogoutAsync(HttpContext httpContext, CancellationToken ct = default);
    Task<string[]> GetAccessCodesAsync(string userId, CancellationToken ct = default);
}
