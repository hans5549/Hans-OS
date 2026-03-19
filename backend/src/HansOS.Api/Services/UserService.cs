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
            Token: string.Empty,
            Email: user.Email ?? string.Empty
        );
    }

    public async Task UpdateProfileAsync(string userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException($"使用者 {userId} 不存在");

        user.RealName = request.RealName;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new ArgumentException(string.Join("；", result.Errors.Select(TranslateIdentityError)));
    }

    public async Task ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException($"使用者 {userId} 不存在");

        var result = await userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(TranslateIdentityError);
            throw new ArgumentException(string.Join("；", errors));
        }
    }

    private static string TranslateIdentityError(IdentityError error) => error.Code switch
    {
        "PasswordTooShort" => "密碼至少需要 8 個字元",
        "PasswordRequiresDigit" => "密碼需要包含數字",
        "PasswordRequiresUpper" => "密碼需要包含大寫字母",
        "PasswordRequiresLower" => "密碼需要包含小寫字母",
        "PasswordRequiresNonAlphanumeric" => "密碼需要包含特殊字元",
        "PasswordRequiresUniqueChars" => "密碼需要包含更多不同字元",
        "PasswordMismatch" => "舊密碼不正確",
        _ => "操作失敗，請稍後再試"
    };
}
