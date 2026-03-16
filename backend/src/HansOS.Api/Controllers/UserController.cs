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
    [HttpGet("info")]
    public async Task<ApiEnvelope<UserInfoResponse>> GetInfo(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var info = await userService.GetUserInfoAsync(userId, ct);
        return ApiEnvelope<UserInfoResponse>.Success(info);
    }
}
