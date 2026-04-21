using HansOS.Api.Data.Entities;
using HansOS.Api.Models.Users;
using Microsoft.AspNetCore.Identity;

namespace HansOS.Api.Services;

public class UserService(
    UserManager<ApplicationUser> userManager) : IUserService
{
    private static readonly HashSet<string> RemovedDashboardHomePaths =
    [
        "/analytics",
        "/dashboard",
        "/todo",
        "/workspace"
    ];

    private static readonly IReadOnlyDictionary<string, string> LegacySystemDesignHomePaths =
        new Dictionary<string, string>
        {
            ["/system-design/qr-code-generator"] = "/system-design/real-world-apps/qr-code-generator",
            ["/system-design/earthquake-notification"] = "/system-design/real-world-apps/earthquake-notification",
            ["/system-design/polymarket"] = "/system-design/real-world-apps/polymarket",
            ["/system-design/amazon-price-tracking"] = "/system-design/real-world-apps/amazon-price-tracking",
            ["/system-design/tesla-robo-taxi"] = "/system-design/real-world-apps/tesla-robo-taxi",
            ["/system-design/spotify-trending-songs"] = "/system-design/real-world-apps/spotify-trending-songs",
            ["/system-design/messenger"] = "/system-design/real-world-apps/messenger",
            ["/system-design/webhook-platform"] = "/system-design/real-world-apps/webhook-platform",
            ["/system-design/google-docs"] = "/system-design/real-world-apps/google-docs",
            ["/system-design/youtube"] = "/system-design/real-world-apps/youtube",
            ["/system-design/chatgpt-tasks"] = "/system-design/real-world-apps/chatgpt-tasks",
            ["/system-design/airbnb-booking"] = "/system-design/real-world-apps/airbnb-booking",
            ["/system-design/agoda-ai-support"] = "/system-design/real-world-apps/agoda-ai-support",
            ["/system-design/llm-inference-api"] = "/system-design/real-world-apps/llm-inference-api",
        };

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
            Desc: user.Desc ?? string.Empty,
            HomePath: NormalizeHomePath(user.HomePath),
            Token: string.Empty,
            Email: user.Email ?? string.Empty,
            Phone: user.PhoneNumber ?? string.Empty
        );
    }

    public async Task UpdateProfileAsync(string userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException($"使用者 {userId} 不存在");

        user.RealName = request.RealName;
        user.Avatar = request.Avatar ?? user.Avatar;
        user.Desc = request.Desc ?? user.Desc;

        if (request.Email is not null && request.Email != user.Email)
        {
            var emailResult = await userManager.SetEmailAsync(user, request.Email);
            if (!emailResult.Succeeded)
                throw new ArgumentException(string.Join("；", emailResult.Errors.Select(TranslateIdentityError)));
        }

        if (request.Phone is not null && request.Phone != user.PhoneNumber)
        {
            var phoneResult = await userManager.SetPhoneNumberAsync(user, request.Phone);
            if (!phoneResult.Succeeded)
                throw new ArgumentException(string.Join("；", phoneResult.Errors.Select(TranslateIdentityError)));
        }

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
            throw new ArgumentException(string.Join("；", result.Errors.Select(TranslateIdentityError)));
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

    private static string NormalizeHomePath(string? homePath)
    {
        if (string.IsNullOrWhiteSpace(homePath) || RemovedDashboardHomePaths.Contains(homePath))
        {
            return "/index";
        }

        if (LegacySystemDesignHomePaths.TryGetValue(homePath, out var normalizedHomePath))
        {
            return normalizedHomePath;
        }

        return homePath;
    }
}
