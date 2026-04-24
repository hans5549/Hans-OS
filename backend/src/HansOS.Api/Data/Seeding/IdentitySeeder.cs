using HansOS.Api.Data.Entities;
using HansOS.Api.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace HansOS.Api.Data.Seeding;

/// <summary>
/// 預設身分資料 Seeder：建立 admin role 與初始管理員帳號。
/// 設計為 idempotent — 重複執行不會建立重複資料。
/// </summary>
public static class IdentitySeeder
{
    private const string DevelopmentAdminPassword = "H@ns19951204";

    public static async Task SeedAsync(IServiceProvider sp, CancellationToken ct = default)
    {
        using var scope = sp.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var seedOptions = scope.ServiceProvider.GetService<IOptions<IdentitySeedOptions>>()?.Value
            ?? new IdentitySeedOptions();
        var environment = scope.ServiceProvider.GetService<IHostEnvironment>();
        var adminPassword = ResolveAdminPassword(seedOptions, environment);

        if (!await roleManager.RoleExistsAsync(seedOptions.AdminRoleName))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole(seedOptions.AdminRoleName));
            if (!roleResult.Succeeded)
                throw new InvalidOperationException(
                    $"建立 admin role 失敗: {string.Join("; ", roleResult.Errors.Select(e => e.Description))}");
        }

        if (await userManager.FindByNameAsync(seedOptions.AdminUserName) is not null)
            return;

        var user = new ApplicationUser
        {
            UserName = seedOptions.AdminUserName,
            RealName = seedOptions.AdminRealName,
            Email = seedOptions.AdminEmail,
            IsActive = true,
            HomePath = seedOptions.AdminHomePath
        };
        var createResult = await userManager.CreateAsync(user, adminPassword);
        if (!createResult.Succeeded)
            throw new InvalidOperationException(
                $"建立 admin user 失敗: {string.Join("; ", createResult.Errors.Select(e => e.Description))}");

        var roleAssignResult = await userManager.AddToRoleAsync(user, seedOptions.AdminRoleName);
        if (!roleAssignResult.Succeeded)
            throw new InvalidOperationException(
                $"指派 admin role 失敗: {string.Join("; ", roleAssignResult.Errors.Select(e => e.Description))}");
    }

    private static string ResolveAdminPassword(IdentitySeedOptions options, IHostEnvironment? environment)
    {
        if (!string.IsNullOrWhiteSpace(options.AdminPassword))
            return options.AdminPassword;

        if (environment?.IsDevelopment() ?? true)
            return DevelopmentAdminPassword;

        throw new InvalidOperationException(
            "非 Development 環境必須設定 IdentitySeed:AdminPassword");
    }
}
