using FluentAssertions;
using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.Auth;
using HansOS.Api.Options;
using HansOS.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace HansOS.Api.UnitTests;

public class AuthServiceTests : IDisposable
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _db;
    private readonly AuthService _sut;
    private readonly JwtOptions _jwtOptions;

    public AuthServiceTests()
    {
        var store = Substitute.For<IUserStore<ApplicationUser>>();
        _userManager = Substitute.For<UserManager<ApplicationUser>>(
            store, null, null, null, null, null, null, null, null);

        var contextAccessor = Substitute.For<IHttpContextAccessor>();
        var claimsFactory = Substitute.For<IUserClaimsPrincipalFactory<ApplicationUser>>();
        _signInManager = Substitute.For<SignInManager<ApplicationUser>>(
            _userManager, contextAccessor, claimsFactory, null, null, null, null);

        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(dbOptions);

        _jwtOptions = new JwtOptions
        {
            Issuer = "test",
            Audience = "test",
            SigningKey = "test-signing-key-that-is-at-least-32-bytes-long!!",
            AccessTokenExpiryMinutes = 30,
            RefreshTokenExpiryDays = 7
        };

        _sut = new AuthService(
            _userManager,
            _signInManager,
            _db,
            Microsoft.Extensions.Options.Options.Create(_jwtOptions),
            Substitute.For<ILogger<AuthService>>());
    }

    private static HttpContext CreateHttpContext(bool isDev = true)
    {
        var httpContext = new DefaultHttpContext();
        var env = Substitute.For<IWebHostEnvironment>();
        env.EnvironmentName.Returns(isDev ? "Development" : "Production");

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(env);
        httpContext.RequestServices = serviceCollection.BuildServiceProvider();

        return httpContext;
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokens()
    {
        var user = new ApplicationUser { Id = "1", UserName = "hans", RealName = "Hans", IsActive = true };
        _userManager.FindByNameAsync("hans").Returns(user);
        _signInManager.CheckPasswordSignInAsync(user, "pass", false)
            .Returns(SignInResult.Success);
        _userManager.GetRolesAsync(user).Returns(new List<string> { "admin" });

        var httpContext = CreateHttpContext();
        var result = await _sut.LoginAsync(new LoginRequest("hans", "pass"), httpContext);

        result.AccessToken.Should().NotBeNullOrEmpty();
        // Refresh token cookie should be set
        httpContext.Response.Headers["Set-Cookie"].ToString().Should().Contain("jwt=");
    }

    [Fact]
    public async Task Login_InvalidPassword_ThrowsUnauthorized()
    {
        var user = new ApplicationUser { Id = "1", UserName = "hans", RealName = "Hans", IsActive = true };
        _userManager.FindByNameAsync("hans").Returns(user);
        _signInManager.CheckPasswordSignInAsync(user, "wrong", false)
            .Returns(SignInResult.Failed);

        var httpContext = CreateHttpContext();
        var act = () => _sut.LoginAsync(new LoginRequest("hans", "wrong"), httpContext);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Login_NonExistentUser_ThrowsUnauthorized()
    {
        _userManager.FindByNameAsync("nobody").Returns((ApplicationUser?)null);

        var httpContext = CreateHttpContext();
        var act = () => _sut.LoginAsync(new LoginRequest("nobody", "pass"), httpContext);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Login_InactiveUser_ThrowsUnauthorized()
    {
        var user = new ApplicationUser { Id = "1", UserName = "hans", RealName = "Hans", IsActive = false };
        _userManager.FindByNameAsync("hans").Returns(user);

        var httpContext = CreateHttpContext();
        var act = () => _sut.LoginAsync(new LoginRequest("hans", "pass"), httpContext);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task RefreshToken_NoCookie_ThrowsUnauthorized()
    {
        var httpContext = CreateHttpContext();
        var act = () => _sut.RefreshTokenAsync(httpContext);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*refresh token*");
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken()
    {
        // Create a token in DB
        var tokenValue = "test-refresh-token";
        var tokenHash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(tokenValue)));

        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        await _db.SaveChangesAsync();

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers.Append("Cookie", $"jwt={tokenValue}");

        await _sut.LogoutAsync(httpContext);

        var storedToken = await _db.RefreshTokens.FirstAsync();
        storedToken.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAccessCodes_ReturnsHardcodedCodes()
    {
        var codes = await _sut.GetAccessCodesAsync("any-user-id");

        codes.Should().NotBeEmpty();
        codes.Should().Contain("AC_100100");
    }

    /// <summary>
    /// 沒有 Cookie 時登出不應拋出例外
    /// </summary>
    [Fact]
    public async Task Logout_WithoutCookie_DoesNotThrow()
    {
        var httpContext = CreateHttpContext();

        var act = () => _sut.LogoutAsync(httpContext);

        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Cookie 含無效 Token 時登出不應拋出例外
    /// </summary>
    [Fact]
    public async Task Logout_WithInvalidToken_DoesNotThrow()
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers.Append("Cookie", "jwt=invalid-token-value");

        var act = () => _sut.LogoutAsync(httpContext);

        await act.Should().NotThrowAsync();
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }
}
