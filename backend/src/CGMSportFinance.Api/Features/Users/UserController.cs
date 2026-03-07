using System.Security.Claims;
using CGMSportFinance.Api.Common;
using CGMSportFinance.Api.Features.Users.Contracts;
using CGMSportFinance.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CGMSportFinance.Api.Features.Users;

[ApiController]
[Authorize]
[Route("api/user")]
public sealed class UserController(UserManager<ApplicationUser> userManager) : ControllerBase
{
    /// <summary>
    /// Return the current authenticated user's profile data for the admin shell.
    /// </summary>
    [HttpGet("info")]
    [ProducesResponseType(typeof(ApiEnvelope<UserInfoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiEnvelope<UserInfoResponse>>> GetInfo()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(Problem(title: "Unauthorized", statusCode: StatusCodes.Status401Unauthorized));
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null || !user.IsActive)
        {
            return Unauthorized(Problem(title: "Unauthorized", statusCode: StatusCodes.Status401Unauthorized));
        }

        var roles = await userManager.GetRolesAsync(user);
        var response = new UserInfoResponse(
            user.Avatar,
            user.HomePath,
            user.RealName,
            roles.ToArray(),
            user.Id,
            user.UserName ?? string.Empty);

        return Ok(ApiEnvelope<UserInfoResponse>.Success(response));
    }
}
