using HansOS.Api.Data.Entities;
using HansOS.Api.Models.Users;
using Microsoft.AspNetCore.Identity;

namespace HansOS.Api.Services;

public class UserService(
    UserManager<ApplicationUser> userManager) : IUserService
{
    public async Task<UserInfoResponse> GetUserInfoAsync(string userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException($"使用者 {userId} 不存在");

        var roles = await userManager.GetRolesAsync(user);

        return new UserInfoResponse(
            UserId: user.Id,
            Username: user.UserName ?? string.Empty,
            RealName: user.RealName,
            Avatar: user.Avatar ?? string.Empty,
            Roles: [.. roles],
            Desc: "管理員",
            HomePath: user.HomePath ?? "/analytics",
            Token: string.Empty // Frontend reads from store, not from this field
        );
    }
}
