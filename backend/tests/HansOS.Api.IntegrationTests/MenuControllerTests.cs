using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HansOS.Api.IntegrationTests;

public class MenuControllerTests(HansOsWebApplicationFactory factory)
    : IClassFixture<HansOsWebApplicationFactory>
{
    private static readonly Guid DashboardMenuId = Guid.Parse("d1e2f3a4-0000-0000-0000-000000000001");
    private static readonly Guid AnalyticsMenuId = Guid.Parse("d1e2f3a4-0000-0000-0000-000000000002");
    private static readonly Guid WorkspaceMenuId = Guid.Parse("d1e2f3a4-0000-0000-0000-000000000003");
    private static readonly Guid TodoMenuId = Guid.Parse("d1e2f3a4-0000-0000-0000-000000000010");
    private readonly HansOsWebApplicationFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetAll_Unauthorized_Returns401()
    {
        var response = await _client.GetAsync("/menu/all");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_Authorized_ReturnsMenuTree()
    {
        await SeedTodoMenuAsync();

        var token = await LoginAndGetTokenAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, "/menu/all");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("code").GetInt32().Should().Be(0);
        var menus = body.GetProperty("data");
        menus.ValueKind.Should().Be(JsonValueKind.Array);

        var dashboardMenu = FindMenuByName(menus, "Dashboard");
        dashboardMenu.ValueKind.Should().NotBe(JsonValueKind.Undefined);

        var todoMenu = FindMenuByName(dashboardMenu.GetProperty("children"), "Todo");
        todoMenu.ValueKind.Should().NotBe(JsonValueKind.Undefined);
        todoMenu.GetProperty("path").GetString().Should().Be("/todo");
        todoMenu.GetProperty("component").GetString().Should().Be("/dashboard/todo/index");
        todoMenu.GetProperty("meta").GetProperty("title").GetString().Should().Be("page.dashboard.todo");
        todoMenu.GetProperty("meta").GetProperty("authority")[0].GetString().Should().Be("admin");
        todoMenu.GetProperty("meta").GetProperty("order").GetInt32().Should().Be(3);
    }

    [Fact]
    public async Task GetAll_NonAdmin_DoesNotReturnTodoMenu()
    {
        await SeedTodoMenuAsync();
        await EnsureUserAsync("viewer", "V!ewer12345", "user");
        await AssignMenusToRoleAsync("user", DashboardMenuId, AnalyticsMenuId, WorkspaceMenuId);

        var token = await LoginAndGetTokenAsync("viewer", "V!ewer12345");

        var request = new HttpRequestMessage(HttpMethod.Get, "/menu/all");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("code").GetInt32().Should().Be(0);

        var menus = body.GetProperty("data");
        var dashboardMenu = FindMenuByName(menus, "Dashboard");
        dashboardMenu.ValueKind.Should().NotBe(JsonValueKind.Undefined);
        ContainsMenuNamed(dashboardMenu.GetProperty("children"), "Analytics").Should().BeTrue();
        ContainsMenuNamed(dashboardMenu.GetProperty("children"), "Workspace").Should().BeTrue();
        ContainsMenuNamed(dashboardMenu.GetProperty("children"), "Todo").Should().BeFalse();
    }

    private static JsonElement FindMenuByName(JsonElement menus, string name)
    {
        foreach (var menu in menus.EnumerateArray())
        {
            if (menu.GetProperty("name").GetString() == name)
            {
                return menu;
            }
        }

        return default;
    }

    private static bool ContainsMenuNamed(JsonElement menus, string name)
    {
        foreach (var menu in menus.EnumerateArray())
        {
            if (menu.GetProperty("name").GetString() == name)
            {
                return true;
            }

            if (menu.TryGetProperty("children", out var children) &&
                children.ValueKind == JsonValueKind.Array &&
                ContainsMenuNamed(children, name))
            {
                return true;
            }
        }

        return false;
    }

    private async Task SeedTodoMenuAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Integration tests use EF InMemory + EnsureCreated, so migration SQL seeds are not applied here.
        // Seed the minimal menu graph needed to verify the /menu/all contract.
        var adminRoleId = await db.Roles
            .Where(role => role.Name == "admin")
            .Select(role => role.Id)
            .SingleAsync();

        if (!await db.Menus.AnyAsync(menu => menu.Id == DashboardMenuId))
        {
            db.Menus.Add(new Menu
            {
                Id = DashboardMenuId,
                Name = "Dashboard",
                Path = "/dashboard",
                Component = "BasicLayout",
                Redirect = "/analytics",
                Title = "page.dashboard.title",
                Type = MenuType.Catalog,
                Order = -1,
            });
        }

        if (!await db.Menus.AnyAsync(menu => menu.Id == AnalyticsMenuId))
        {
            db.Menus.Add(new Menu
            {
                Id = AnalyticsMenuId,
                ParentId = DashboardMenuId,
                Name = "Analytics",
                Path = "/analytics",
                Component = "/dashboard/analytics/index",
                Title = "page.dashboard.analytics",
                Type = MenuType.Menu,
                Order = 1,
                AffixTab = true,
            });
        }

        if (!await db.Menus.AnyAsync(menu => menu.Id == TodoMenuId))
        {
            db.Menus.Add(new Menu
            {
                Id = TodoMenuId,
                ParentId = DashboardMenuId,
                Name = "Todo",
                Path = "/todo",
                Component = "/dashboard/todo/index",
                Title = "page.dashboard.todo",
                Authority = """["admin"]""",
                Type = MenuType.Menu,
                Order = 3,
            });
        }

        if (!await db.Menus.AnyAsync(menu => menu.Id == WorkspaceMenuId))
        {
            db.Menus.Add(new Menu
            {
                Id = WorkspaceMenuId,
                ParentId = DashboardMenuId,
                Name = "Workspace",
                Path = "/workspace",
                Component = "/dashboard/workspace/index",
                Title = "page.dashboard.workspace",
                Type = MenuType.Menu,
                Order = 2,
            });
        }

        if (!await db.RoleMenus.AnyAsync(roleMenu => roleMenu.RoleId == adminRoleId && roleMenu.MenuId == AnalyticsMenuId))
        {
            db.RoleMenus.Add(new RoleMenu
            {
                RoleId = adminRoleId,
                MenuId = AnalyticsMenuId,
            });
        }

        if (!await db.RoleMenus.AnyAsync(roleMenu => roleMenu.RoleId == adminRoleId && roleMenu.MenuId == DashboardMenuId))
        {
            db.RoleMenus.Add(new RoleMenu
            {
                RoleId = adminRoleId,
                MenuId = DashboardMenuId,
            });
        }

        if (!await db.RoleMenus.AnyAsync(roleMenu => roleMenu.RoleId == adminRoleId && roleMenu.MenuId == WorkspaceMenuId))
        {
            db.RoleMenus.Add(new RoleMenu
            {
                RoleId = adminRoleId,
                MenuId = WorkspaceMenuId,
            });
        }

        if (!await db.RoleMenus.AnyAsync(roleMenu => roleMenu.RoleId == adminRoleId && roleMenu.MenuId == TodoMenuId))
        {
            db.RoleMenus.Add(new RoleMenu
            {
                RoleId = adminRoleId,
                MenuId = TodoMenuId,
            });
        }

        await db.SaveChangesAsync();
    }

    private async Task EnsureUserAsync(string username, string password, string roleName)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var createRoleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
            createRoleResult.Succeeded.Should().BeTrue();
        }

        var existingUser = await userManager.FindByNameAsync(username);
        if (existingUser is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            Email = $"{username}@example.com",
            EmailConfirmed = true,
            UserName = username
        };

        var createUserResult = await userManager.CreateAsync(user, password);
        createUserResult.Succeeded.Should().BeTrue();

        var addToRoleResult = await userManager.AddToRoleAsync(user, roleName);
        addToRoleResult.Succeeded.Should().BeTrue();
    }

    private async Task AssignMenusToRoleAsync(string roleName, params Guid[] menuIds)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleId = await db.Roles
            .Where(role => role.Name == roleName)
            .Select(role => role.Id)
            .SingleAsync();

        foreach (var menuId in menuIds)
        {
            if (!await db.RoleMenus.AnyAsync(roleMenu => roleMenu.RoleId == roleId && roleMenu.MenuId == menuId))
            {
                db.RoleMenus.Add(new RoleMenu
                {
                    RoleId = roleId,
                    MenuId = menuId,
                });
            }
        }

        await db.SaveChangesAsync();
    }

    private async Task<string> LoginAndGetTokenAsync(
        string username = "hans",
        string password = "H@ns19951204")
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new
        {
            username,
            password
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("code").GetInt32().Should().Be(0);

        var accessToken = body.GetProperty("data").GetProperty("accessToken").GetString();
        accessToken.Should().NotBeNullOrWhiteSpace();

        return accessToken!;
    }
}
