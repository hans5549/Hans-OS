using HansOS.Api.Infrastructure.Identity;

namespace HansOS.Api.Features.Auth;

public interface ITokenService
{
    TokenIssueResult CreateAccessToken(ApplicationUser user, IEnumerable<string> roles);

    RefreshTokenIssue CreateRefreshToken(string userId, string? ipAddress, string? userAgent);

    CookieOptions CreateRefreshTokenCookie(bool isSecureEnvironment);

    string HashRefreshToken(string rawToken);
}
