using CGMSportFinance.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CGMSportFinance.Api.Infrastructure.Persistence.Seeding;

public sealed class DatabaseSeeder(
    ApplicationDbContext dbContext,
    RoleManager<IdentityRole> roleManager,
    UserManager<ApplicationUser> userManager,
    IWebHostEnvironment environment,
    IOptions<SeedingOptions> seedingOptions,
    IOptions<BootstrapAdminOptions> bootstrapAdminOptions) : IDatabaseSeeder
{
    private const string SuperRoleName = "super";
    private const string AdminRoleName = "admin";
    private const string UserRoleName = "user";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (dbContext.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
        {
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        }
        else
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }

        var roles = await EnsureRolesAsync();
        await EnsureUsersAsync(cancellationToken);
        var permissions = await EnsurePermissionsAsync(cancellationToken);
        var menus = await EnsureMenusAsync(cancellationToken);

        await EnsureRolePermissionsAsync(roles, permissions, cancellationToken);
        await EnsureRoleMenusAsync(roles, menus, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Dictionary<string, Menu>> EnsureMenusAsync(CancellationToken cancellationToken)
    {
        var menus = new[]
        {
            new MenuSeed(
                Id: Guid.Parse("8C37E615-4CD6-4BF7-BE2A-690B87D8F001"),
                Name: "Dashboard",
                Path: "/dashboard",
                TitleKey: "page.dashboard.title",
                Type: MenuType.Catalog,
                OrderNo: -1,
                Icon: "lucide:layout-dashboard",
                Redirect: "/analytics"),
            new MenuSeed(
                Id: Guid.Parse("8C37E615-4CD6-4BF7-BE2A-690B87D8F002"),
                ParentId: Guid.Parse("8C37E615-4CD6-4BF7-BE2A-690B87D8F001"),
                Name: "Analytics",
                Path: "/analytics",
                TitleKey: "page.dashboard.analytics",
                Type: MenuType.Menu,
                OrderNo: 0,
                Icon: "lucide:area-chart",
                ComponentKey: "/dashboard/analytics/index",
                KeepAlive: true,
                AffixTab: true,
                PermissionCode: "dashboard:analytics:view"),
            new MenuSeed(
                Id: Guid.Parse("8C37E615-4CD6-4BF7-BE2A-690B87D8F003"),
                ParentId: Guid.Parse("8C37E615-4CD6-4BF7-BE2A-690B87D8F001"),
                Name: "Workspace",
                Path: "/workspace",
                TitleKey: "page.dashboard.workspace",
                Type: MenuType.Menu,
                OrderNo: 1,
                Icon: "carbon:workspace",
                ComponentKey: "/dashboard/workspace/index",
                PermissionCode: "dashboard:workspace:view"),
            new MenuSeed(
                Id: Guid.Parse("8C37E615-4CD6-4BF7-BE2A-690B87D8F004"),
                Name: "Demos",
                Path: "/demos",
                TitleKey: "demos.title",
                Type: MenuType.Catalog,
                OrderNo: 1000,
                Icon: "ic:baseline-view-in-ar",
                KeepAlive: true),
            new MenuSeed(
                Id: Guid.Parse("8C37E615-4CD6-4BF7-BE2A-690B87D8F005"),
                ParentId: Guid.Parse("8C37E615-4CD6-4BF7-BE2A-690B87D8F004"),
                Name: "AntDesignDemos",
                Path: "/demos/ant-design",
                TitleKey: "demos.antd",
                Type: MenuType.Menu,
                OrderNo: 0,
                ComponentKey: "/demos/antd/index",
                PermissionCode: "demos:ant-design:view"),
            new MenuSeed(
                Id: Guid.Parse("8C37E615-4CD6-4BF7-BE2A-690B87D8F006"),
                Name: "VbenProject",
                Path: "/vben-admin",
                TitleKey: "demos.vben.title",
                Type: MenuType.Catalog,
                OrderNo: 9998,
                Icon: "lucide:panels-top-left"),
            new MenuSeed(
                Id: Guid.Parse("8C37E615-4CD6-4BF7-BE2A-690B87D8F007"),
                ParentId: Guid.Parse("8C37E615-4CD6-4BF7-BE2A-690B87D8F006"),
                Name: "VbenDocument",
                Path: "/vben-admin/document",
                TitleKey: "demos.vben.document",
                Type: MenuType.Menu,
                OrderNo: 0,
                Icon: "lucide:book-open-text",
                ComponentKey: "IFrameView",
                Link: "https://doc.vben.pro/",
                PermissionCode: "vben:document:view"),
            new MenuSeed(
                Id: Guid.Parse("8C37E615-4CD6-4BF7-BE2A-690B87D8F008"),
                ParentId: Guid.Parse("8C37E615-4CD6-4BF7-BE2A-690B87D8F006"),
                Name: "VbenGithub",
                Path: "/vben-admin/github",
                TitleKey: "Github",
                Type: MenuType.Menu,
                OrderNo: 1,
                Icon: "mdi:github",
                ComponentKey: "IFrameView",
                Link: "https://github.com/vbenjs/vue-vben-admin"),
            new MenuSeed(
                Id: Guid.Parse("8C37E615-4CD6-4BF7-BE2A-690B87D8F009"),
                Name: "VbenAbout",
                Path: "/vben-admin/about",
                TitleKey: "demos.vben.about",
                Type: MenuType.Menu,
                OrderNo: 9999,
                Icon: "lucide:copyright",
                ComponentKey: "/_core/about/index",
                PermissionCode: "vben:about:view"),
            new MenuSeed(
                Id: Guid.Parse("8C37E615-4CD6-4BF7-BE2A-690B87D8F010"),
                Name: "Profile",
                Path: "/profile",
                TitleKey: "page.auth.profile",
                Type: MenuType.Menu,
                OrderNo: 10000,
                Icon: "lucide:user",
                ComponentKey: "/_core/profile/index",
                HideInMenu: true,
                PermissionCode: "profile:view"),
        };

        var menuIds = menus.Select(menu => menu.Id).ToArray();
        var existing = await dbContext.Menus.Where(menu => menuIds.Contains(menu.Id)).ToDictionaryAsync(menu => menu.Id, cancellationToken);

        foreach (var seed in menus)
        {
            if (!existing.TryGetValue(seed.Id, out var menu))
            {
                menu = new Menu { Id = seed.Id };
                dbContext.Menus.Add(menu);
                existing[seed.Id] = menu;
            }

            menu.ActivePath = seed.ActivePath;
            menu.AffixTab = seed.AffixTab;
            menu.Badge = seed.Badge;
            menu.BadgeType = seed.BadgeType;
            menu.BadgeVariant = seed.BadgeVariant;
            menu.ComponentKey = seed.ComponentKey;
            menu.HideInBreadcrumb = seed.HideInBreadcrumb;
            menu.HideInMenu = seed.HideInMenu;
            menu.HideInTab = seed.HideInTab;
            menu.Icon = seed.Icon;
            menu.Id = seed.Id;
            menu.IframeSrc = seed.IframeSrc;
            menu.KeepAlive = seed.KeepAlive;
            menu.Link = seed.Link;
            menu.MenuVisibleWithForbidden = seed.MenuVisibleWithForbidden;
            menu.Name = seed.Name;
            menu.OrderNo = seed.OrderNo;
            menu.ParentId = seed.ParentId;
            menu.Path = seed.Path;
            menu.PermissionCode = seed.PermissionCode;
            menu.Redirect = seed.Redirect;
            menu.Status = true;
            menu.TitleKey = seed.TitleKey;
            menu.Type = seed.Type;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return await dbContext.Menus.ToDictionaryAsync(menu => menu.Name, cancellationToken);
    }

    private async Task<Dictionary<string, Permission>> EnsurePermissionsAsync(CancellationToken cancellationToken)
    {
        var seedPermissions = new[]
        {
            new PermissionSeed("dashboard:analytics:view", "View analytics dashboard"),
            new PermissionSeed("dashboard:workspace:view", "View workspace dashboard"),
            new PermissionSeed("demos:ant-design:view", "View Ant Design demo page"),
            new PermissionSeed("vben:document:view", "View Vben documentation iframe"),
            new PermissionSeed("vben:about:view", "View About page"),
            new PermissionSeed("profile:view", "View profile page"),
            new PermissionSeed("dashboard:analytics:refresh", "Refresh analytics widgets"),
            new PermissionSeed("system:menu:create", "Create menu items"),
            new PermissionSeed("system:menu:edit", "Edit menu items"),
            new PermissionSeed("system:menu:delete", "Delete menu items"),
        };

        var codes = seedPermissions.Select(permission => permission.Code).ToArray();
        var existing = await dbContext.Permissions.Where(permission => codes.Contains(permission.Code)).ToDictionaryAsync(permission => permission.Code, cancellationToken);

        foreach (var seed in seedPermissions)
        {
            if (!existing.TryGetValue(seed.Code, out var permission))
            {
                permission = new Permission { Code = seed.Code };
                dbContext.Permissions.Add(permission);
                existing[seed.Code] = permission;
            }

            permission.Description = seed.Description;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return existing;
    }

    private async Task EnsureRoleMenusAsync(
        IReadOnlyDictionary<string, IdentityRole> roles,
        IReadOnlyDictionary<string, Menu> menus,
        CancellationToken cancellationToken)
    {
        var assignments = new Dictionary<string, string[]>
        {
            [SuperRoleName] =
            [
                "Dashboard",
                "Analytics",
                "Workspace",
                "Demos",
                "AntDesignDemos",
                "VbenProject",
                "VbenDocument",
                "VbenGithub",
                "VbenAbout",
                "Profile",
            ],
            [AdminRoleName] =
            [
                "Dashboard",
                "Analytics",
                "Workspace",
                "Demos",
                "AntDesignDemos",
                "VbenAbout",
                "Profile",
            ],
            [UserRoleName] =
            [
                "Dashboard",
                "Analytics",
                "Profile",
            ],
        };

        foreach (var (roleName, menuNames) in assignments)
        {
            var role = roles[roleName];
            var menuIds = menuNames.Select(name => menus[name].Id).ToHashSet();
            var existingMappings = await dbContext.RoleMenus
                .Where(mapping => mapping.RoleId == role.Id)
                .ToListAsync(cancellationToken);

            foreach (var mapping in existingMappings.Where(mapping => !menuIds.Contains(mapping.MenuId)))
            {
                dbContext.RoleMenus.Remove(mapping);
            }

            var existingMenuIds = existingMappings.Select(mapping => mapping.MenuId).ToHashSet();
            foreach (var menuId in menuIds.Where(menuId => !existingMenuIds.Contains(menuId)))
            {
                dbContext.RoleMenus.Add(new RoleMenu
                {
                    MenuId = menuId,
                    RoleId = role.Id,
                });
            }
        }
    }

    private async Task EnsureRolePermissionsAsync(
        IReadOnlyDictionary<string, IdentityRole> roles,
        IReadOnlyDictionary<string, Permission> permissions,
        CancellationToken cancellationToken)
    {
        var assignments = new Dictionary<string, string[]>
        {
            [SuperRoleName] = permissions.Keys.ToArray(),
            [AdminRoleName] =
            [
                "dashboard:analytics:view",
                "dashboard:workspace:view",
                "demos:ant-design:view",
                "vben:about:view",
                "profile:view",
                "dashboard:analytics:refresh",
                "system:menu:create",
                "system:menu:edit",
            ],
            [UserRoleName] =
            [
                "dashboard:analytics:view",
                "profile:view",
            ],
        };

        foreach (var (roleName, codes) in assignments)
        {
            var role = roles[roleName];
            var permissionIds = codes.Select(code => permissions[code].Id).ToHashSet();
            var existingMappings = await dbContext.RolePermissions
                .Where(mapping => mapping.RoleId == role.Id)
                .ToListAsync(cancellationToken);

            foreach (var mapping in existingMappings.Where(mapping => !permissionIds.Contains(mapping.PermissionId)))
            {
                dbContext.RolePermissions.Remove(mapping);
            }

            var existingPermissionIds = existingMappings.Select(mapping => mapping.PermissionId).ToHashSet();
            foreach (var permissionId in permissionIds.Where(permissionId => !existingPermissionIds.Contains(permissionId)))
            {
                dbContext.RolePermissions.Add(new RolePermission
                {
                    PermissionId = permissionId,
                    RoleId = role.Id,
                });
            }
        }
    }

    private async Task<Dictionary<string, IdentityRole>> EnsureRolesAsync()
    {
        var roleNames = new[] { SuperRoleName, AdminRoleName, UserRoleName };
        var roles = new Dictionary<string, IdentityRole>(StringComparer.OrdinalIgnoreCase);

        foreach (var roleName in roleNames)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                role = new IdentityRole(roleName);
                await EnsureSucceededAsync(roleManager.CreateAsync(role), $"create role '{roleName}'");
            }

            roles[roleName] = role;
        }

        return roles;
    }

    private async Task EnsureUsersAsync(CancellationToken cancellationToken)
    {
        if (ShouldSeedDemoUsers())
        {
            await EnsureDemoUsersAsync(cancellationToken);
        }

        if (!environment.IsProduction())
        {
            return;
        }

        if (bootstrapAdminOptions.Value.IsConfigured())
        {
            await EnsureBootstrapAdminAsync(cancellationToken);
            return;
        }

        if (!await HasPrivilegedUserAsync(cancellationToken))
        {
            throw new InvalidOperationException("BootstrapAdmin configuration is required in Production when no privileged user exists.");
        }
    }

    private bool ShouldSeedDemoUsers()
    {
        return !environment.IsProduction() || seedingOptions.Value.EnableDemoData;
    }

    private async Task EnsureDemoUsersAsync(CancellationToken cancellationToken)
    {
        var seedUsers = new[]
        {
            new SeedUser("vben", "123456", "Vben", "/analytics", "https://ui-avatars.com/api/?name=Vben&background=0D8ABC&color=fff", "负责体育金融运营与数据分析。", new[] { SuperRoleName }),
            new SeedUser("admin", "123456", "Admin", "/workspace", "https://ui-avatars.com/api/?name=Admin&background=1F8A70&color=fff", "管理后台账号与基础配置维护。", new[] { AdminRoleName }),
            new SeedUser("jack", "123456", "Jack", "/analytics", "https://ui-avatars.com/api/?name=Jack&background=7C3AED&color=fff", "关注日报表、任务提醒与基础运营指标。", new[] { UserRoleName }),
        };

        foreach (var seed in seedUsers)
        {
            var user = await userManager.FindByNameAsync(seed.Username);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    Avatar = seed.Avatar,
                    Email = $"{seed.Username}@example.local",
                    EmailConfirmed = true,
                    HomePath = seed.HomePath,
                    IsActive = true,
                    RealName = seed.RealName,
                    UserName = seed.Username,
                };

                await EnsureSucceededAsync(userManager.CreateAsync(user, seed.Password), $"create user '{seed.Username}'");
            }
            else
            {
                user.Avatar = seed.Avatar;
                user.HomePath = seed.HomePath;
                user.IsActive = true;
                user.RealName = seed.RealName;
                user.Email ??= $"{seed.Username}@example.local";
                user.EmailConfirmed = true;
                await EnsureSucceededAsync(userManager.UpdateAsync(user), $"update user '{seed.Username}'");
            }

            var currentRoles = await userManager.GetRolesAsync(user);
            var rolesToAdd = seed.Roles.Except(currentRoles, StringComparer.OrdinalIgnoreCase).ToArray();
            var rolesToRemove = currentRoles.Except(seed.Roles, StringComparer.OrdinalIgnoreCase).ToArray();

            if (rolesToRemove.Length > 0)
            {
                await EnsureSucceededAsync(userManager.RemoveFromRolesAsync(user, rolesToRemove), $"remove stale roles from user '{seed.Username}'");
            }

            if (rolesToAdd.Length > 0)
            {
                await EnsureSucceededAsync(userManager.AddToRolesAsync(user, rolesToAdd), $"assign roles to user '{seed.Username}'");
            }

            await EnsureUserProfileAsync(user, seed.Introduction, cancellationToken);
        }
    }

    private async Task EnsureBootstrapAdminAsync(CancellationToken cancellationToken)
    {
        var options = bootstrapAdminOptions.Value;
        var user = await FindBootstrapAdminAsync(options);

        if (user is null)
        {
            user = new ApplicationUser
            {
                Avatar = options.Avatar,
                Email = options.Email,
                EmailConfirmed = true,
                HomePath = options.HomePath,
                IsActive = true,
                RealName = options.RealName,
                UserName = options.Username,
            };

            await EnsureSucceededAsync(userManager.CreateAsync(user, options.Password), $"create bootstrap admin '{options.Username}'");
        }
        else
        {
            user.Email = options.Email;
            user.EmailConfirmed = true;
            user.IsActive = true;
            user.UserName = options.Username;

            if (string.IsNullOrWhiteSpace(user.RealName))
            {
                user.RealName = options.RealName;
            }

            if (string.IsNullOrWhiteSpace(user.HomePath))
            {
                user.HomePath = options.HomePath;
            }

            if (string.IsNullOrWhiteSpace(user.Avatar) && !string.IsNullOrWhiteSpace(options.Avatar))
            {
                user.Avatar = options.Avatar;
            }

            await EnsureSucceededAsync(userManager.UpdateAsync(user), $"update bootstrap admin '{options.Username}'");

            if (!await userManager.CheckPasswordAsync(user, options.Password))
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                await EnsureSucceededAsync(
                    userManager.ResetPasswordAsync(user, token, options.Password),
                    $"sync bootstrap admin password for '{options.Username}'");
            }
        }

        if (!await userManager.IsInRoleAsync(user, SuperRoleName))
        {
            await EnsureSucceededAsync(userManager.AddToRoleAsync(user, SuperRoleName), $"assign '{SuperRoleName}' role to bootstrap admin '{options.Username}'");
        }

        await EnsureUserProfileAsync(user, "系统引导管理员，可维护基础设置与账号资料。", cancellationToken);
    }

    private async Task<ApplicationUser?> FindBootstrapAdminAsync(BootstrapAdminOptions options)
    {
        var user = await userManager.FindByNameAsync(options.Username);
        if (user is not null)
        {
            return user;
        }

        return await userManager.FindByEmailAsync(options.Email);
    }

    private async Task<bool> HasPrivilegedUserAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var superUsers = await userManager.GetUsersInRoleAsync(SuperRoleName);
        if (superUsers.Count > 0)
        {
            return true;
        }

        var adminUsers = await userManager.GetUsersInRoleAsync(AdminRoleName);
        return adminUsers.Count > 0;
    }

    private static async Task EnsureSucceededAsync(Task<IdentityResult> operation, string action)
    {
        var result = await operation;
        if (result.Succeeded)
        {
            return;
        }

        throw new InvalidOperationException($"Failed to {action}: {string.Join(", ", result.Errors.Select(error => error.Description))}");
    }

    private async Task EnsureUserProfileAsync(ApplicationUser user, string introduction, CancellationToken cancellationToken)
    {
        var profile = await dbContext.UserProfiles.SingleOrDefaultAsync(item => item.UserId == user.Id, cancellationToken);
        if (profile is null)
        {
            profile = new UserProfile
            {
                UserId = user.Id,
            };
            dbContext.UserProfiles.Add(profile);
        }

        if (string.IsNullOrWhiteSpace(profile.Introduction))
        {
            profile.Introduction = introduction;
        }

        profile.NotifyAccountPassword = true;
        profile.NotifySystemMessage = true;
        profile.NotifyTodoTask = true;
    }

    private sealed record MenuSeed(
        Guid Id,
        string Name,
        string Path,
        string TitleKey,
        MenuType Type,
        int OrderNo,
        Guid? ParentId = null,
        string? ComponentKey = null,
        string? Redirect = null,
        string? Icon = null,
        string? PermissionCode = null,
        string? Link = null,
        string? IframeSrc = null,
        bool KeepAlive = false,
        bool HideInMenu = false,
        bool HideInTab = false,
        bool HideInBreadcrumb = false,
        bool AffixTab = false,
        string? ActivePath = null,
        string? Badge = null,
        string? BadgeType = null,
        string? BadgeVariant = null,
        bool MenuVisibleWithForbidden = false);

    private sealed record PermissionSeed(string Code, string Description);

    private sealed record SeedUser(
        string Username,
        string Password,
        string RealName,
        string HomePath,
        string Avatar,
        string Introduction,
        string[] Roles);
}
