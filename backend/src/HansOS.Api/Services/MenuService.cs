using System.Text.Json;
using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.Menus;
using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.Services;

public class MenuService(ApplicationDbContext db) : IMenuService
{
    public async Task<List<MenuRouteResponse>> GetMenusByRolesAsync(IList<string> roles, CancellationToken ct = default)
    {
        // Resolve role names to role IDs (JWT carries names, RoleMenus stores IDs)
        var roleIds = await db.Roles
            .AsNoTracking()
            .Where(r => roles.Contains(r.Name!))
            .Select(r => r.Id)
            .ToListAsync(ct);

        // Get all menu IDs assigned to user's roles
        var menuIds = await db.RoleMenus
            .AsNoTracking()
            .Where(rm => roleIds.Contains(rm.RoleId))
            .Select(rm => rm.MenuId)
            .Distinct()
            .ToListAsync(ct);

        var menus = await LoadMenusWithAncestorsAsync(menuIds, ct);

        return BuildTree(menus, null);
    }

    private async Task<List<Menu>> LoadMenusWithAncestorsAsync(
        IReadOnlyCollection<Guid> menuIds,
        CancellationToken ct)
    {
        var menus = await db.Menus
            .AsNoTracking()
            .Where(menu => menuIds.Contains(menu.Id) && menu.IsActive && menu.Type != MenuType.Button)
            .OrderBy(menu => menu.Order)
            .ToListAsync(ct);

        var loadedMenuIds = menus.Select(menu => menu.Id).ToHashSet();
        var missingParentIds = GetMissingParentIds(menus, loadedMenuIds);

        while (missingParentIds.Count > 0)
        {
            var parentMenus = await db.Menus
                .AsNoTracking()
                .Where(menu => missingParentIds.Contains(menu.Id) && menu.IsActive && menu.Type != MenuType.Button)
                .ToListAsync(ct);

            if (parentMenus.Count == 0)
            {
                break;
            }

            menus.AddRange(parentMenus);

            foreach (var parentMenu in parentMenus)
            {
                loadedMenuIds.Add(parentMenu.Id);
            }

            missingParentIds = GetMissingParentIds(menus, loadedMenuIds);
        }

        return menus;
    }

    private static HashSet<Guid> GetMissingParentIds(
        IEnumerable<Menu> menus,
        IReadOnlySet<Guid> loadedMenuIds)
    {
        return menus
            .Where(menu => menu.ParentId.HasValue && !loadedMenuIds.Contains(menu.ParentId.Value))
            .Select(menu => menu.ParentId!.Value)
            .ToHashSet();
    }

    private static List<MenuRouteResponse> BuildTree(List<Menu> menus, Guid? parentId)
    {
        return menus
            .Where(m => m.ParentId == parentId)
            .OrderBy(m => m.Order)
            .Select(m =>
            {
                var children = BuildTree(menus, m.Id);
                return new MenuRouteResponse
                {
                    Name = m.Name,
                    Path = m.Path,
                    Component = m.Component ?? string.Empty,
                    Redirect = m.Redirect,
                    Meta = new MenuMetaResponse
                    {
                        Title = m.Title,
                        Icon = m.Icon,
                        ActiveIcon = m.ActiveIcon,
                        Order = m.Order,
                        AffixTab = m.AffixTab,
                        AffixTabOrder = m.AffixTabOrder,
                        HideInMenu = m.HideInMenu,
                        HideInTab = m.HideInTab,
                        HideInBreadcrumb = m.HideInBreadcrumb,
                        HideChildrenInMenu = m.HideChildrenInMenu,
                        KeepAlive = m.KeepAlive,
                        Authority = ParseAuthority(m.Authority),
                        Badge = m.Badge,
                        BadgeType = m.BadgeType,
                        BadgeVariants = m.BadgeVariants,
                        Link = m.Link,
                        IframeSrc = m.IframeSrc,
                        NoBasicLayout = m.NoBasicLayout,
                        MaxNumOfOpenTab = m.MaxNumOfOpenTab
                    },
                    Children = children.Count > 0 ? children : null
                };
            })
            .ToList();
    }

    private static string[]? ParseAuthority(string? authority)
    {
        if (string.IsNullOrWhiteSpace(authority)) return null;
        try { return JsonSerializer.Deserialize<string[]>(authority); }
        catch { return null; }
    }
}
