using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HansOS.Api.IntegrationTests;

public class MenuControllerTests(HansOsWebApplicationFactory factory)
    : IClassFixture<HansOsWebApplicationFactory>
{
    private static readonly Guid PreservedMenuId = Guid.Parse("f1e2d3c4-0000-0000-0000-000000000001");
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetAll_Unauthorized_Returns401()
    {
        var response = await _client.GetAsync("/menu/all");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_Authorized_DoesNotReturnRemovedDashboardMenus()
    {
        await SeedPreservedMenuAsync();
        var token = await LoginAndGetTokenAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, "/menu/all");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("code").GetInt32().Should().Be(0);
        var menus = body.GetProperty("data");
        menus.ValueKind.Should().Be(JsonValueKind.Array);
        ContainsMenuNamed(menus, "FinanceReports").Should().BeTrue();
        ContainsMenuNamed(menus, "Dashboard").Should().BeFalse();
        ContainsMenuNamed(menus, "Analytics").Should().BeFalse();
        ContainsMenuNamed(menus, "Workspace").Should().BeFalse();
        ContainsMenuNamed(menus, "Todo").Should().BeFalse();
    }

    private async Task SeedPreservedMenuAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var adminRoleId = await db.Roles
            .Where(role => role.Name == "admin")
            .Select(role => role.Id)
            .SingleAsync();

        if (!await db.Menus.AnyAsync(menu => menu.Id == PreservedMenuId))
        {
            db.Menus.Add(new Menu
            {
                Id = PreservedMenuId,
                Name = "FinanceReports",
                Path = "/finance/reports",
                Component = "/finance/reports/index",
                Title = "page.finance.reports",
                Type = MenuType.Menu,
                Order = 1,
            });
        }

        if (!await db.RoleMenus.AnyAsync(roleMenu => roleMenu.RoleId == adminRoleId && roleMenu.MenuId == PreservedMenuId))
        {
            db.RoleMenus.Add(new RoleMenu
            {
                RoleId = adminRoleId,
                MenuId = PreservedMenuId,
            });
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
}
