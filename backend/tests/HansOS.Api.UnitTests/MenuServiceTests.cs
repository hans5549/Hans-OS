using FluentAssertions;
using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.UnitTests;

public class MenuServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly MenuService _sut;

    public MenuServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);
        _sut = new MenuService(_db);
    }

    [Fact]
    public async Task GetMenusByRoles_AdminRole_ReturnsAllMenus()
    {
        // Arrange
        var roleId = "admin-role-id";
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        _db.Menus.AddRange(
            new Menu
            {
                Id = parentId, Name = "Dashboard", Path = "/dashboard",
                Component = "BasicLayout", Title = "Dashboard",
                Type = MenuType.Catalog, Order = 1
            },
            new Menu
            {
                Id = childId, ParentId = parentId, Name = "Analytics", Path = "/analytics",
                Component = "/dashboard/analytics", Title = "Analytics",
                Type = MenuType.Menu, Order = 1
            });

        _db.RoleMenus.AddRange(
            new RoleMenu { RoleId = roleId, MenuId = parentId },
            new RoleMenu { RoleId = roleId, MenuId = childId });

        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.GetMenusByRolesAsync(["admin-role-id"]);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Dashboard");
        result[0].Children.Should().HaveCount(1);
        result[0].Children![0].Name.Should().Be("Analytics");
    }

    [Fact]
    public async Task GetMenusByRoles_BuildsCorrectTree()
    {
        var roleId = "role-1";
        var rootId = Guid.NewGuid();
        var child1Id = Guid.NewGuid();
        var child2Id = Guid.NewGuid();

        _db.Menus.AddRange(
            new Menu { Id = rootId, Name = "Root", Path = "/root", Component = "BasicLayout", Title = "Root", Type = MenuType.Catalog, Order = 1 },
            new Menu { Id = child1Id, ParentId = rootId, Name = "Child1", Path = "/c1", Component = "/c1", Title = "C1", Type = MenuType.Menu, Order = 1 },
            new Menu { Id = child2Id, ParentId = rootId, Name = "Child2", Path = "/c2", Component = "/c2", Title = "C2", Type = MenuType.Menu, Order = 2 });

        _db.RoleMenus.AddRange(
            new RoleMenu { RoleId = roleId, MenuId = rootId },
            new RoleMenu { RoleId = roleId, MenuId = child1Id },
            new RoleMenu { RoleId = roleId, MenuId = child2Id });

        await _db.SaveChangesAsync();

        var result = await _sut.GetMenusByRolesAsync([roleId]);

        result.Should().HaveCount(1);
        result[0].Children.Should().HaveCount(2);
        result[0].Children![0].Name.Should().Be("Child1");
        result[0].Children![1].Name.Should().Be("Child2");
    }

    [Fact]
    public async Task GetMenusByRoles_NoMatchingMenus_ReturnsEmpty()
    {
        var result = await _sut.GetMenusByRolesAsync(["nonexistent-role"]);
        result.Should().BeEmpty();
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }
}
