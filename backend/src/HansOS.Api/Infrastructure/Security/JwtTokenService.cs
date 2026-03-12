using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HansOS.Api.Features.Auth;
using HansOS.Api.Infrastructure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HansOS.Api.Infrastructure.Security;

public sealed class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    private readonly JwtOptions jwtOptions = options.Value;

    public TokenIssueResult CreateAccessToken(ApplicationUser user, IEnumerable<string> roles)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(jwtOptions.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var descriptor = new SecurityTokenDescriptor
        {
            Audience = jwtOptions.Audience,
            Expires = expiresAt,
            Issuer = jwtOptions.Issuer,
            Subject = new ClaimsIdentity(claims),
            SigningCredentials = credentials,
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(descriptor);
        var accessToken = handler.WriteToken(token);

        return new TokenIssueResult(accessToken, (int)TimeSpan.FromMinutes(jwtOptions.AccessTokenMinutes).TotalSeconds);
    }

    public RefreshTokenIssue CreateRefreshToken(string userId, string? ipAddress, string? userAgent)
    {
        Span<byte> tokenBytes = stackalloc byte[64];
        RandomNumberGenerator.Fill(tokenBytes);
        var rawToken = Base64UrlEncoder.Encode(tokenBytes.ToArray());

        var entity = new RefreshToken
        {
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByIp = ipAddress,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(jwtOptions.RefreshTokenDays),
            Id = Guid.NewGuid(),
            TokenHash = HashRefreshToken(rawToken),
            UserAgent = userAgent ?? string.Empty,
            UserId = userId,
        };

        return new RefreshTokenIssue(rawToken, entity);
    }

    public CookieOptions CreateRefreshTokenCookie(bool isSecureEnvironment)
    {
        return new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddDays(jwtOptions.RefreshTokenDays),
            HttpOnly = true,
            IsEssential = true,
            Path = "/",
            SameSite = isSecureEnvironment ? SameSiteMode.None : SameSiteMode.Lax,
            Secure = isSecureEnvironment,
        };
    }

    public string HashRefreshToken(string rawToken)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hashBytes);
    }
}
