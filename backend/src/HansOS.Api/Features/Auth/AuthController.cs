using HansOS.Api.Common;
using HansOS.Api.Features.Auth.Contracts;
using HansOS.Api.Infrastructure.Identity;
using HansOS.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.Features.Auth;

[ApiController]
[Route("auth")]
public sealed class AuthController(
    ApplicationDbContext dbContext,
    IHostEnvironment hostEnvironment,
    ITokenService tokenService,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiEnvelope<AuthTokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiEnvelope<AuthTokenResponse>>> Login([FromBody] LoginRequest request)
    {
        var user = await userManager.FindByNameAsync(request.Username);
        if (user is null || !user.IsActive)
            return InvalidCredentials();

        var passwordValid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
            return InvalidCredentials();

        var roles = await userManager.GetRolesAsync(user);
        var accessToken = tokenService.CreateAccessToken(user, roles);
        var refreshToken = tokenService.CreateRefreshToken(
            user.Id,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        dbContext.RefreshTokens.Add(refreshToken.RefreshToken);
        await dbContext.SaveChangesAsync();

        WriteRefreshTokenCookie(refreshToken.PlainTextToken);

        return Ok(ApiEnvelope<AuthTokenResponse>.Success(
            new AuthTokenResponse(accessToken.AccessToken, accessToken.ExpiresIn)));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiEnvelope<AuthTokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiEnvelope<AuthTokenResponse>>> Refresh()
    {
        var rawToken = Request.Cookies[RefreshTokenCookie.CookieName];
        if (string.IsNullOrWhiteSpace(rawToken))
            return Unauthorized(Problem(title: "Unauthorized", statusCode: StatusCodes.Status401Unauthorized));

        var tokenHash = tokenService.HashRefreshToken(rawToken);
        var existingToken = await dbContext.RefreshTokens
            .Include(token => token.User)
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash);

        if (existingToken is null || !existingToken.IsActive || !existingToken.User.IsActive)
        {
            ClearRefreshTokenCookie();
            return Unauthorized(Problem(title: "Unauthorized", statusCode: StatusCodes.Status401Unauthorized));
        }

        existingToken.RevokedAt = DateTimeOffset.UtcNow;
        existingToken.RevokedByIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        var replacement = tokenService.CreateRefreshToken(
            existingToken.UserId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        existingToken.ReplacedByTokenId = replacement.RefreshToken.Id;
        dbContext.RefreshTokens.Add(replacement.RefreshToken);

        var roles = await userManager.GetRolesAsync(existingToken.User);
        var accessToken = tokenService.CreateAccessToken(existingToken.User, roles);

        await dbContext.SaveChangesAsync();

        WriteRefreshTokenCookie(replacement.PlainTextToken);

        return Ok(ApiEnvelope<AuthTokenResponse>.Success(
            new AuthTokenResponse(accessToken.AccessToken, accessToken.ExpiresIn)));
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    [ProducesResponseType(typeof(ApiEnvelope<object?>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiEnvelope<object?>>> Logout()
    {
        var rawToken = Request.Cookies[RefreshTokenCookie.CookieName];
        if (!string.IsNullOrWhiteSpace(rawToken))
        {
            var tokenHash = tokenService.HashRefreshToken(rawToken);
            var existingToken = await dbContext.RefreshTokens
                .SingleOrDefaultAsync(token => token.TokenHash == tokenHash);

            if (existingToken is not null && existingToken.IsActive)
            {
                existingToken.RevokedAt = DateTimeOffset.UtcNow;
                existingToken.RevokedByIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                await dbContext.SaveChangesAsync();
            }
        }

        ClearRefreshTokenCookie();
        return Ok(ApiEnvelope<object?>.Success(null));
    }

    [Authorize]
    [HttpGet("codes")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyCollection<string>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyCollection<string>>>> GetAccessCodes()
    {
        var roleNames = User.FindAll(System.Security.Claims.ClaimTypes.Role)
            .Select(claim => claim.Value).Distinct().ToArray();

        var codes = await dbContext.RolePermissions
            .AsNoTracking()
            .Where(mapping => roleNames.Contains(mapping.Role.Name!))
            .Select(mapping => mapping.Permission.Code)
            .Distinct()
            .OrderBy(code => code)
            .ToArrayAsync();

        return Ok(ApiEnvelope<IReadOnlyCollection<string>>.Success(codes));
    }

    private ActionResult<ApiEnvelope<AuthTokenResponse>> InvalidCredentials() =>
        StatusCode(StatusCodes.Status403Forbidden,
            Problem(title: "Forbidden", detail: "Username or password is incorrect.",
                statusCode: StatusCodes.Status403Forbidden));

    private void ClearRefreshTokenCookie() =>
        Response.Cookies.Delete(RefreshTokenCookie.CookieName, new CookieOptions
        {
            HttpOnly = true,
            Path = "/",
            SameSite = hostEnvironment.IsProduction() ? SameSiteMode.None : SameSiteMode.Lax,
            Secure = hostEnvironment.IsProduction(),
        });

    private void WriteRefreshTokenCookie(string token) =>
        Response.Cookies.Append(RefreshTokenCookie.CookieName, token,
            tokenService.CreateRefreshTokenCookie(hostEnvironment.IsProduction()));
}
