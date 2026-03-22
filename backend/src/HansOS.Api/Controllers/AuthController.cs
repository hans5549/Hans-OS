using System.Security.Claims;
using HansOS.Api.Common;
using HansOS.Api.Models.Auth;
using HansOS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HansOS.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ApiEnvelope<LoginResponse>> Login(
        [FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await authService.LoginAsync(request, HttpContext, ct);
        return ApiEnvelope<LoginResponse>.Success(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<string> Refresh(CancellationToken ct)
    {
        return await authService.RefreshTokenAsync(HttpContext, ct);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<ApiEnvelope<string>> Logout(CancellationToken ct)
    {
        await authService.LogoutAsync(HttpContext, ct);
        return ApiEnvelope<string>.Success("");
    }

    [HttpGet("codes")]
    [Authorize]
    public async Task<ApiEnvelope<string[]>> GetCodes(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var codes = await authService.GetAccessCodesAsync(userId, ct);
        return ApiEnvelope<string[]>.Success(codes);
    }
}
