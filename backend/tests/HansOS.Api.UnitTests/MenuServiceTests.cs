using FluentAssertions;
using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Services;
using Microsoft.AspNetCore.Identity;
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

        _db.Roles.Add(new IdentityRole { Id = roleId, Name = "admin", NormalizedName = "ADMIN" });

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

        // Act — pass role name (as JWT ClaimTypes.Role provides names, not IDs)
        var result = await _sut.GetMenusByRolesAsync(["admin"]);

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

        _db.Roles.Add(new IdentityRole { Id = roleId, Name = "editor", NormalizedName = "EDITOR" });

        _db.Menus.AddRange(
            new Menu { Id = rootId, Name = "Root", Path = "/root", Component = "BasicLayout", Title = "Root", Type = MenuType.Catalog, Order = 1 },
            new Menu { Id = child1Id, ParentId = rootId, Name = "Child1", Path = "/c1", Component = "/c1", Title = "C1", Type = MenuType.Menu, Order = 1 },
            new Menu { Id = child2Id, ParentId = rootId, Name = "Child2", Path = "/c2", Component = "/c2", Title = "C2", Type = MenuType.Menu, Order = 2 });

        _db.RoleMenus.AddRange(
            new RoleMenu { RoleId = roleId, MenuId = rootId },
            new RoleMenu { RoleId = roleId, MenuId = child1Id },
            new RoleMenu { RoleId = roleId, MenuId = child2Id });

        await _db.SaveChangesAsync();

        // Pass role name, not role ID
        var result = await _sut.GetMenusByRolesAsync(["editor"]);

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

    [Fact]
    public async Task GetMenusByRoles_MultipleRoles_MergesMenusCorrectly()
    {
        // Arrange — 兩個角色各自擁有部分選單，其中一個選單同時指派給兩個角色
        var adminRoleId = "admin-role-id";
        var editorRoleId = "editor-role-id";
        var sharedMenuId = Guid.NewGuid();
        var adminOnlyMenuId = Guid.NewGuid();
        var editorOnlyMenuId = Guid.NewGuid();

        _db.Roles.AddRange(
            new IdentityRole { Id = adminRoleId, Name = "Admin", NormalizedName = "ADMIN" },
            new IdentityRole { Id = editorRoleId, Name = "Editor", NormalizedName = "EDITOR" });

        _db.Menus.AddRange(
            new Menu
            {
                Id = sharedMenuId, Name = "Shared", Path = "/shared",
                Component = "/shared/index", Title = "Shared",
                Type = MenuType.Menu, Order = 1
            },
            new Menu
            {
                Id = adminOnlyMenuId, Name = "AdminOnly", Path = "/admin-only",
                Component = "/admin/index", Title = "Admin Only",
                Type = MenuType.Menu, Order = 2
            },
            new Menu
            {
                Id = editorOnlyMenuId, Name = "EditorOnly", Path = "/editor-only",
                Component = "/editor/index", Title = "Editor Only",
                Type = MenuType.Menu, Order = 3
            });

        _db.RoleMenus.AddRange(
            new RoleMenu { RoleId = adminRoleId, MenuId = sharedMenuId },
            new RoleMenu { RoleId = editorRoleId, MenuId = sharedMenuId },
            new RoleMenu { RoleId = adminRoleId, MenuId = adminOnlyMenuId },
            new RoleMenu { RoleId = editorRoleId, MenuId = editorOnlyMenuId });

        await _db.SaveChangesAsync();

        // Act — 同時傳入兩個角色名稱
        var result = await _sut.GetMenusByRolesAsync(["Admin", "Editor"]);

        // Assert — 三個選單都應回傳，且共用選單不重複
        result.Should().HaveCount(3);
        result.Select(r => r.Name).Should().BeEquivalentTo(["Shared", "AdminOnly", "EditorOnly"]);
    }

    [Fact]
    public async Task GetMenusByRoles_DeeplyNestedMenus_BuildsCorrectTree()
    {
        // Arrange — 三層深度：祖父 → 父層 → 子層
        var roleId = "deep-role-id";
        var grandparentId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        _db.Roles.Add(new IdentityRole { Id = roleId, Name = "viewer", NormalizedName = "VIEWER" });

        _db.Menus.AddRange(
            new Menu
            {
                Id = grandparentId, Name = "Level1", Path = "/level1",
                Component = "BasicLayout", Title = "Level 1",
                Type = MenuType.Catalog, Order = 1
            },
            new Menu
            {
                Id = parentId, ParentId = grandparentId, Name = "Level2", Path = "/level2",
                Component = "/level2/index", Title = "Level 2",
                Type = MenuType.Catalog, Order = 1
            },
            new Menu
            {
                Id = childId, ParentId = parentId, Name = "Level3", Path = "/level3",
                Component = "/level3/index", Title = "Level 3",
                Type = MenuType.Menu, Order = 1
            });

        // 三層都指派給角色
        _db.RoleMenus.AddRange(
            new RoleMenu { RoleId = roleId, MenuId = grandparentId },
            new RoleMenu { RoleId = roleId, MenuId = parentId },
            new RoleMenu { RoleId = roleId, MenuId = childId });

        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.GetMenusByRolesAsync(["viewer"]);

        // Assert — 驗證三層樹結構
        result.Should().HaveCount(1);
        var level1 = result[0];
        level1.Name.Should().Be("Level1");
        level1.Children.Should().HaveCount(1);

        var level2 = level1.Children![0];
        level2.Name.Should().Be("Level2");
        level2.Children.Should().HaveCount(1);

        var level3 = level2.Children![0];
        level3.Name.Should().Be("Level3");
        level3.Children.Should().BeNull();
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }
}
