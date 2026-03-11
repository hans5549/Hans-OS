using System.Security.Claims;
using CGMSportFinance.Api.Common;
using CGMSportFinance.Api.Features.Profile.Contracts;
using CGMSportFinance.Api.Infrastructure.Identity;
using CGMSportFinance.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CGMSportFinance.Api.Features.Profile;

[ApiController]
[Authorize]
[Route("api/profile")]
public sealed class ProfileController(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiEnvelope<ProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiEnvelope<ProfileResponse>>> GetProfile(CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized(Problem(title: "Unauthorized", statusCode: StatusCodes.Status401Unauthorized));
        }

        var roles = await userManager.GetRolesAsync(user);
        var response = MapResponse(user, roles.ToArray());
        return Ok(ApiEnvelope<ProfileResponse>.Success(response));
    }

    [HttpPut("basic")]
    [ProducesResponseType(typeof(ApiEnvelope<ProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiEnvelope<ProfileResponse>>> UpdateBasic(
        [FromBody] UpdateProfileBasicRequest request,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized(Problem(title: "Unauthorized", statusCode: StatusCodes.Status401Unauthorized));
        }

        var profile = await EnsureProfileAsync(user, cancellationToken);
        user.RealName = request.RealName.Trim();
        user.Email = request.Email.Trim();
        user.PhoneNumber = request.PhoneNumber.Trim();
        profile.Introduction = request.Introduction.Trim();

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }

            return ValidationProblem(ModelState);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var roles = await userManager.GetRolesAsync(user);
        var response = MapResponse(user, roles.ToArray());
        return Ok(ApiEnvelope<ProfileResponse>.Success(response));
    }

    [HttpPut("notifications")]
    [ProducesResponseType(typeof(ApiEnvelope<ProfileNotificationsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiEnvelope<ProfileNotificationsResponse>>> UpdateNotifications(
        [FromBody] UpdateProfileNotificationsRequest request,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized(Problem(title: "Unauthorized", statusCode: StatusCodes.Status401Unauthorized));
        }

        var profile = await EnsureProfileAsync(user, cancellationToken);
        profile.NotifyAccountPassword = request.NotifyAccountPassword;
        profile.NotifySystemMessage = request.NotifySystemMessage;
        profile.NotifyTodoTask = request.NotifyTodoTask;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ApiEnvelope<ProfileNotificationsResponse>.Success(
            new ProfileNotificationsResponse(
                profile.NotifyAccountPassword,
                profile.NotifySystemMessage,
                profile.NotifyTodoTask)));
    }

    [HttpPost("change-password")]
    [ProducesResponseType(typeof(ApiEnvelope<object?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiEnvelope<object?>>> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized(Problem(title: "Unauthorized", statusCode: StatusCodes.Status401Unauthorized));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var result = await userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }

            return ValidationProblem(ModelState);
        }

        return Ok(ApiEnvelope<object?>.Success(null));
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var user = await dbContext.Users
            .Include(item => item.Profile)
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return null;
        }

        return user;
    }

    private async Task<UserProfile> EnsureProfileAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        if (user.Profile is not null)
        {
            return user.Profile;
        }

        var profile = new UserProfile
        {
            UserId = user.Id,
        };

        dbContext.UserProfiles.Add(profile);
        user.Profile = profile;
        await dbContext.SaveChangesAsync(cancellationToken);
        return profile;
    }

    private static ProfileResponse MapResponse(ApplicationUser user, IReadOnlyCollection<string> roles)
    {
        var profile = user.Profile;
        var introduction = profile?.Introduction ?? string.Empty;

        return new ProfileResponse(
            new ProfileBasicResponse(
                user.Email ?? string.Empty,
                introduction,
                user.PhoneNumber ?? string.Empty,
                user.RealName,
                roles,
                user.UserName ?? string.Empty),
            new ProfileHeaderResponse(
                user.Avatar,
                user.RealName,
                roles,
                user.Id,
                user.UserName ?? string.Empty),
            new ProfileNotificationsResponse(
                profile?.NotifyAccountPassword ?? true,
                profile?.NotifySystemMessage ?? true,
                profile?.NotifyTodoTask ?? true),
            new ProfileSecurityResponse(
                !string.IsNullOrWhiteSpace(user.Email),
                !string.IsNullOrWhiteSpace(user.PasswordHash),
                !string.IsNullOrWhiteSpace(user.PhoneNumber),
                user.TwoFactorEnabled));
    }
}
