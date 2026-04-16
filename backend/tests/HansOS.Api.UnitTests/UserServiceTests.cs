using FluentAssertions;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.Users;
using HansOS.Api.Services;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace HansOS.Api.UnitTests;

public class UserServiceTests
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly UserService _sut;

    public UserServiceTests()
    {
        var store = Substitute.For<IUserStore<ApplicationUser>>();
        _userManager = Substitute.For<UserManager<ApplicationUser>>(
            store, null, null, null, null, null, null, null, null);
        _sut = new UserService(_userManager);
    }

    [Fact]
    public async Task GetUserInfo_ExistingUser_ReturnsCorrectFields()
    {
        var user = new ApplicationUser
        {
            Id = "user-1",
            UserName = "hans",
            RealName = "Hans",
            Avatar = "https://example.com/avatar.png",
            HomePath = "/custom-home",
            IsActive = true
        };
        _userManager.FindByIdAsync("user-1").Returns(user);
        _userManager.GetRolesAsync(user).Returns(new List<string> { "admin" });

        var result = await _sut.GetUserInfoAsync("user-1");

        result.UserId.Should().Be("user-1");
        result.Username.Should().Be("hans");
        result.RealName.Should().Be("Hans");
        result.Avatar.Should().Be("https://example.com/avatar.png");
        result.Roles.Should().Contain("admin");
        result.HomePath.Should().Be("/custom-home");
    }

    [Fact]
    public async Task GetUserInfo_NonExistentUser_ThrowsNotFound()
    {
        _userManager.FindByIdAsync("nonexistent").Returns((ApplicationUser?)null);

        var act = () => _sut.GetUserInfoAsync("nonexistent");

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── UpdateProfileAsync ─────────────────────────

    [Fact]
    public async Task UpdateProfile_ValidData_UpdatesSuccessfully()
    {
        var user = new ApplicationUser
        {
            Id = "user-1",
            UserName = "hans",
            RealName = "Hans",
            Email = "old@example.com",
        };
        _userManager.FindByIdAsync("user-1").Returns(user);
        _userManager.SetEmailAsync(user, "new@example.com")
            .Returns(IdentityResult.Success);
        _userManager.UpdateAsync(user).Returns(IdentityResult.Success);

        var request = new UpdateProfileRequest(
            RealName: "Hans Updated",
            Email: "new@example.com",
            Phone: null,
            Avatar: null,
            Desc: "new desc");

        await _sut.UpdateProfileAsync("user-1", request);

        user.RealName.Should().Be("Hans Updated");
        user.Desc.Should().Be("new desc");
    }

    [Fact]
    public async Task UpdateProfile_NonExistentUser_ThrowsNotFound()
    {
        _userManager.FindByIdAsync("nonexistent").Returns((ApplicationUser?)null);

        var request = new UpdateProfileRequest(
            RealName: "Any",
            Email: null,
            Phone: null,
            Avatar: null,
            Desc: null);

        var act = () => _sut.UpdateProfileAsync("nonexistent", request);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateProfile_DuplicateEmail_ThrowsError()
    {
        var user = new ApplicationUser
        {
            Id = "user-1",
            UserName = "hans",
            RealName = "Hans",
            Email = "old@example.com",
        };
        _userManager.FindByIdAsync("user-1").Returns(user);
        _userManager.SetEmailAsync(user, "taken@example.com")
            .Returns(IdentityResult.Failed(new IdentityError
            {
                Code = "DuplicateEmail",
                Description = "Email already taken",
            }));

        var request = new UpdateProfileRequest(
            RealName: "Hans",
            Email: "taken@example.com",
            Phone: null,
            Avatar: null,
            Desc: null);

        var act = () => _sut.UpdateProfileAsync("user-1", request);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ── ChangePasswordAsync ────────────────────────

    [Fact]
    public async Task ChangePassword_ValidData_ChangesSuccessfully()
    {
        var user = new ApplicationUser { Id = "user-1", UserName = "hans", RealName = "Hans" };
        _userManager.FindByIdAsync("user-1").Returns(user);
        _userManager.ChangePasswordAsync(user, "OldPass1!", "NewPass1!")
            .Returns(IdentityResult.Success);

        var request = new ChangePasswordRequest("OldPass1!", "NewPass1!");

        var act = () => _sut.ChangePasswordAsync("user-1", request);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ChangePassword_WrongOldPassword_ThrowsError()
    {
        var user = new ApplicationUser { Id = "user-1", UserName = "hans", RealName = "Hans" };
        _userManager.FindByIdAsync("user-1").Returns(user);
        _userManager.ChangePasswordAsync(user, "wrong", "NewPass1!")
            .Returns(IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordMismatch",
                Description = "Incorrect password.",
            }));

        var request = new ChangePasswordRequest("wrong", "NewPass1!");

        var act = () => _sut.ChangePasswordAsync("user-1", request);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*舊密碼不正確*");
    }

    [Fact]
    public async Task ChangePassword_NonExistentUser_ThrowsNotFound()
    {
        _userManager.FindByIdAsync("nonexistent").Returns((ApplicationUser?)null);

        var request = new ChangePasswordRequest("old", "NewPass1!");

        var act = () => _sut.ChangePasswordAsync("nonexistent", request);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── 邊界測試 ───────────────────────────────────

    [Fact]
    public async Task GetUserInfo_UserWithNoRoles_ReturnsEmptyRolesArray()
    {
        // 使用者沒有任何角色時，應回傳空陣列而非錯誤
        var user = new ApplicationUser
        {
            Id = "user-no-roles",
            UserName = "noroles",
            RealName = "No Roles User",
            Avatar = null,
            HomePath = null,
            Email = null,
            PhoneNumber = null,
            Desc = null,
            IsActive = true
        };
        _userManager.FindByIdAsync("user-no-roles").Returns(user);
        _userManager.GetRolesAsync(user).Returns(new List<string>());

        var result = await _sut.GetUserInfoAsync("user-no-roles");

        result.Roles.Should().NotBeNull();
        result.Roles.Should().BeEmpty();
        result.Username.Should().Be("noroles");
        result.Avatar.Should().Be(string.Empty);
        result.HomePath.Should().Be("/index");
        result.Email.Should().Be(string.Empty);
        result.Phone.Should().Be(string.Empty);
        result.Desc.Should().Be(string.Empty);
    }

    [Theory]
    [InlineData("/analytics")]
    [InlineData("/dashboard")]
    [InlineData("/todo")]
    [InlineData("/workspace")]
    public async Task GetUserInfo_LegacyDashboardHomePath_NormalizesToIndex(string legacyHomePath)
    {
        var user = new ApplicationUser
        {
            Id = "legacy-user",
            UserName = "legacy",
            HomePath = legacyHomePath,
            IsActive = true
        };
        _userManager.FindByIdAsync("legacy-user").Returns(user);
        _userManager.GetRolesAsync(user).Returns(new List<string>());

        var result = await _sut.GetUserInfoAsync("legacy-user");

        result.HomePath.Should().Be("/index");
    }

    [Theory]
    [InlineData("/system-design/qr-code-generator", "/system-design/real-world-apps/qr-code-generator")]
    [InlineData("/system-design/earthquake-notification", "/system-design/real-world-apps/earthquake-notification")]
    [InlineData("/system-design/polymarket", "/system-design/real-world-apps/polymarket")]
    [InlineData("/system-design/amazon-price-tracking", "/system-design/real-world-apps/amazon-price-tracking")]
    [InlineData("/system-design/tesla-robo-taxi", "/system-design/real-world-apps/tesla-robo-taxi")]
    [InlineData("/system-design/spotify-trending-songs", "/system-design/real-world-apps/spotify-trending-songs")]
    [InlineData("/system-design/messenger", "/system-design/real-world-apps/messenger")]
    [InlineData("/system-design/webhook-platform", "/system-design/real-world-apps/webhook-platform")]
    [InlineData("/system-design/google-docs", "/system-design/real-world-apps/google-docs")]
    [InlineData("/system-design/youtube", "/system-design/real-world-apps/youtube")]
    [InlineData("/system-design/chatgpt-tasks", "/system-design/real-world-apps/chatgpt-tasks")]
    [InlineData("/system-design/airbnb-booking", "/system-design/real-world-apps/airbnb-booking")]
    [InlineData("/system-design/agoda-ai-support", "/system-design/real-world-apps/agoda-ai-support")]
    [InlineData("/system-design/llm-inference-api", "/system-design/real-world-apps/llm-inference-api")]
    public async Task GetUserInfo_LegacySystemDesignHomePath_NormalizesToReorganizedPath(
        string legacyHomePath,
        string expectedHomePath)
    {
        var user = new ApplicationUser
        {
            Id = "legacy-system-design-user",
            UserName = "legacy-system-design",
            HomePath = legacyHomePath,
            IsActive = true,
        };
        _userManager.FindByIdAsync("legacy-system-design-user").Returns(user);
        _userManager.GetRolesAsync(user).Returns(new List<string>());

        var result = await _sut.GetUserInfoAsync("legacy-system-design-user");

        result.HomePath.Should().Be(expectedHomePath);
    }
}
