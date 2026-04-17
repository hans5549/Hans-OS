using HansOS.Api.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace HansOS.Api.Data.Seeding;

/// <summary>
/// 預設身分資料 Seeder：建立 admin role 與初始管理員帳號。
/// 設計為 idempotent — 重複執行不會建立重複資料。
/// </summary>
public static class IdentitySeeder
{
    private const string AdminRoleName = "admin";
    private const string AdminUserName = "hans";
    private const string AdminPassword = "H@ns19951204";

    public static async Task SeedAsync(IServiceProvider sp, CancellationToken ct = default)
    {
        using var scope = sp.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roleManager.RoleExistsAsync(AdminRoleName))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole(AdminRoleName));
            if (!roleResult.Succeeded)
                throw new InvalidOperationException(
                    $"建立 admin role 失敗: {string.Join("; ", roleResult.Errors.Select(e => e.Description))}");
        }

        if (await userManager.FindByNameAsync(AdminUserName) is not null)
            return;

        var user = new ApplicationUser
        {
            UserName = AdminUserName,
            RealName = "Hans",
            Email = "hans@hans-os.dev",
            IsActive = true,
            HomePath = "/index"
        };
        var createResult = await userManager.CreateAsync(user, AdminPassword);
        if (!createResult.Succeeded)
            throw new InvalidOperationException(
                $"建立 admin user 失敗: {string.Join("; ", createResult.Errors.Select(e => e.Description))}");

        var roleAssignResult = await userManager.AddToRoleAsync(user, AdminRoleName);
        if (!roleAssignResult.Succeeded)
            throw new InvalidOperationException(
                $"指派 admin role 失敗: {string.Join("; ", roleAssignResult.Errors.Select(e => e.Description))}");
    }
}
