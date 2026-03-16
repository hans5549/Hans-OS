using FluentAssertions;
using HansOS.Api.Data.Entities;
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
            HomePath = "/analytics",
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
        result.HomePath.Should().Be("/analytics");
    }

    [Fact]
    public async Task GetUserInfo_NonExistentUser_ThrowsNotFound()
    {
        _userManager.FindByIdAsync("nonexistent").Returns((ApplicationUser?)null);

        var act = () => _sut.GetUserInfoAsync("nonexistent");

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
