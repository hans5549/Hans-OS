using System.Security.Claims;
using HansOS.Api.Common;
using HansOS.Api.Models.Users;
using HansOS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HansOS.Api.Controllers;

[ApiController]
[Route("user")]
[Authorize]
public class UserController(IUserService userService) : ControllerBase
{
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("info")]
    public async Task<ApiEnvelope<UserInfoResponse>> GetInfo(CancellationToken ct)
    {
        var info = await userService.GetUserInfoAsync(CurrentUserId, ct);
        return ApiEnvelope<UserInfoResponse>.Success(info);
    }

    [HttpPut("profile")]
    public async Task<ApiEnvelope<object?>> UpdateProfile(
        [FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        await userService.UpdateProfileAsync(CurrentUserId, request, ct);
        return ApiEnvelope<object?>.Success(null);
    }

    [HttpPost("change-password")]
    public async Task<ApiEnvelope<object?>> ChangePassword(
        [FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        await userService.ChangePasswordAsync(CurrentUserId, request, ct);
        return ApiEnvelope<object?>.Success(null);
    }
}
