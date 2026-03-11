using CGMSportFinance.Api.Infrastructure.Identity;
using CGMSportFinance.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CGMSportFinance.Api.IntegrationTests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _sqlitePath = Path.Combine(Path.GetTempPath(), $"cgmsportfinance-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Database:Provider", "Sqlite");
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_sqlitePath}");
        builder.UseSetting("Frontend:AllowedOrigins:0", "http://localhost:5666");
        builder.UseSetting("Jwt:SigningKey", "integration-tests-signing-key-32chars-minimum");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        // CreateHost is a synchronous override; sync-over-async is unavoidable here.
        SeedTestDataAsync(host.Services).GetAwaiter().GetResult();
        return host;
    }

    private static async Task SeedTestDataAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await EnsureRolesAsync(roleManager);
        await EnsureUsersAsync(userManager);
        await EnsureDashboardMenuAsync(roleManager, dbContext);
    }

    private static readonly string[] RoleNames = ["super", "admin", "user"];

    private static async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var roleName in RoleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }

    private static async Task EnsureUsersAsync(UserManager<ApplicationUser> userManager)
    {
        await CreateUserAsync(userManager, "admin", "Test Admin", "super");
        await CreateUserAsync(userManager, "vben", "Test Vben", "user");
    }

    private static async Task CreateUserAsync(
        UserManager<ApplicationUser> userManager,
        string userName,
        string realName,
        string role)
    {
        var user = new ApplicationUser
        {
            UserName = userName,
            RealName = realName,
            HomePath = "/workspace",
            Avatar = string.Empty,
            IsActive = true,
        };
        var createResult = await userManager.CreateAsync(user, "123456");
        if (!createResult.Succeeded)
            throw new InvalidOperationException($"Failed to create test user '{userName}': {string.Join(", ", createResult.Errors.Select(e => e.Description))}");

        var roleResult = await userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
            throw new InvalidOperationException($"Failed to assign role '{role}' to '{userName}': {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
    }

    private static async Task EnsureDashboardMenuAsync(
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext dbContext)
    {
        var superRole = await roleManager.FindByNameAsync("super")
            ?? throw new InvalidOperationException("Role 'super' not found. EnsureRolesAsync must run first.");

        var menu = new Menu
        {
            Id = Guid.NewGuid(),
            Name = "Dashboard",
            Path = "/dashboard",
            TitleKey = "page.dashboard.title",
            Type = MenuType.Menu,
            OrderNo = 1,
        };

        dbContext.Menus.Add(menu);
        dbContext.RoleMenus.Add(new RoleMenu { RoleId = superRole.Id, MenuId = menu.Id });
        await dbContext.SaveChangesAsync();
    }
}
