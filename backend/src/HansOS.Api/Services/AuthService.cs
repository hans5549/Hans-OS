using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.Auth;
using HansOS.Api.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HansOS.Api.Services;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ApplicationDbContext db,
    IOptions<JwtOptions> jwtOptions,
    ILogger<AuthService> logger) : IAuthService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;
    private const string CookieName = "jwt";

    public async Task<LoginResponse> LoginAsync(LoginRequest request, HttpContext httpContext, CancellationToken ct = default)
    {
        var user = await userManager.FindByNameAsync(request.Username);
        if (user is null || !user.IsActive)
            throw new UnauthorizedAccessException("帳號或密碼錯誤");

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
            throw new UnauthorizedAccessException("帳號或密碼錯誤");

        var roles = await userManager.GetRolesAsync(user);
        var accessToken = CreateAccessToken(user, roles);
        var refreshToken = GenerateRefreshToken();

        await SaveRefreshTokenAsync(user.Id, refreshToken, ct);
        SetRefreshTokenCookie(httpContext, refreshToken);

        logger.LogInformation("使用者 {Username} 登入成功", user.UserName);

        return new LoginResponse(accessToken);
    }

    public async Task<string> RefreshTokenAsync(HttpContext httpContext, CancellationToken ct = default)
    {
        var oldToken = httpContext.Request.Cookies[CookieName]
            ?? throw new UnauthorizedAccessException("缺少 refresh token");

        var tokenHash = HashToken(oldToken);
        var stored = await db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct)
            ?? throw new UnauthorizedAccessException("無效的 refresh token");

        if (!stored.IsActive)
            throw new UnauthorizedAccessException("Refresh token 已過期或已撤銷");

        // Revoke old, issue new
        stored.RevokedAt = DateTime.UtcNow;

        var user = stored.User;
        var roles = await userManager.GetRolesAsync(user);
        var newAccessToken = CreateAccessToken(user, roles);
        var newRefreshToken = GenerateRefreshToken();

        await SaveRefreshTokenAsync(user.Id, newRefreshToken, ct);
        SetRefreshTokenCookie(httpContext, newRefreshToken);

        logger.LogInformation("使用者 {UserId} refresh token 更新成功", user.Id);

        return newAccessToken;
    }

    public async Task LogoutAsync(HttpContext httpContext, CancellationToken ct = default)
    {
        var token = httpContext.Request.Cookies[CookieName];
        if (token is not null)
        {
            var tokenHash = HashToken(token);
            var stored = await db.RefreshTokens
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

            if (stored is not null)
            {
                stored.RevokedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
            }
        }

        httpContext.Response.Cookies.Delete(CookieName, GetCookieOptions(httpContext));
    }

    public Task<string[]> GetAccessCodesAsync(string userId, CancellationToken ct = default)
    {
        // Hardcoded for single-user personal project (Linus simplification)
        string[] codes =
        [
            "AC_100100", "AC_100110", "AC_100120", "AC_100010"
        ];
        return Task.FromResult(codes);
    }

    private string CreateAccessToken(ApplicationUser user, IList<string> roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("realName", user.RealName)
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwt.AccessTokenExpiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }

    private async Task SaveRefreshTokenAsync(string userId, string token, CancellationToken ct)
    {
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = HashToken(token),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshTokenExpiryDays)
        });
        await db.SaveChangesAsync(ct);
    }

    private void SetRefreshTokenCookie(HttpContext httpContext, string token)
    {
        httpContext.Response.Cookies.Append(CookieName, token, GetCookieOptions(httpContext));
    }

    private CookieOptions GetCookieOptions(HttpContext httpContext)
    {
        var isDev = httpContext.RequestServices
            .GetRequiredService<IWebHostEnvironment>().IsDevelopment();

        return new CookieOptions
        {
            HttpOnly = true,
            Secure = !isDev,
            SameSite = isDev ? SameSiteMode.Lax : SameSiteMode.None,
            MaxAge = TimeSpan.FromDays(_jwt.RefreshTokenExpiryDays),
            Path = "/"
        };
    }
}
