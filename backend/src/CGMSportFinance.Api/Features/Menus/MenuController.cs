using System.Security.Claims;
using CGMSportFinance.Api.Common;
using CGMSportFinance.Api.Features.Menus.Contracts;
using CGMSportFinance.Api.Infrastructure.Identity;
using CGMSportFinance.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CGMSportFinance.Api.Features.Menus;

[ApiController]
[Authorize]
[Route("api/menu")]
public sealed class MenuController(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    /// <summary>
    /// Return the current user's route tree for backend-driven navigation.
    /// </summary>
    [HttpGet("all")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyCollection<MenuRouteResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyCollection<MenuRouteResponse>>>> GetAll()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(Problem(title: "Unauthorized", statusCode: StatusCodes.Status401Unauthorized));
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null || !user.IsActive)
        {
            return Unauthorized(Problem(title: "Unauthorized", statusCode: StatusCodes.Status401Unauthorized));
        }

        var roleNames = await userManager.GetRolesAsync(user);
        var assignedMenus = await dbContext.RoleMenus
            .AsNoTracking()
            .Where(mapping => roleNames.Contains(mapping.Role.Name!))
            .Select(mapping => mapping.Menu)
            .Where(menu => menu.Status && menu.Type != MenuType.Button)
            .Distinct()
            .OrderBy(menu => menu.OrderNo)
            .ToListAsync();

        var menusByParent = assignedMenus
            .GroupBy(menu => menu.ParentId ?? Guid.Empty)
            .ToDictionary(group => group.Key, group => group.OrderBy(menu => menu.OrderNo).ToList());

        var tree = BuildTree(Guid.Empty, menusByParent);
        return Ok(ApiEnvelope<IReadOnlyCollection<MenuRouteResponse>>.Success(tree));
    }

    private static List<MenuRouteResponse> BuildTree(
        Guid parentId,
        IReadOnlyDictionary<Guid, List<Menu>> menusByParent)
    {
        if (!menusByParent.TryGetValue(parentId, out var children))
        {
            return [];
        }

        return children.Select(menu => new MenuRouteResponse
            {
                Children = BuildTree(menu.Id, menusByParent),
                Component = menu.ComponentKey,
                Meta = new MenuMetaResponse
                {
                    ActivePath = menu.ActivePath,
                    AffixTab = menu.AffixTab,
                    Badge = menu.Badge,
                    BadgeType = menu.BadgeType,
                    BadgeVariants = menu.BadgeVariant,
                    HideInBreadcrumb = menu.HideInBreadcrumb,
                    HideInMenu = menu.HideInMenu,
                    HideInTab = menu.HideInTab,
                    Icon = menu.Icon,
                    IframeSrc = menu.IframeSrc,
                    KeepAlive = menu.KeepAlive,
                    Link = menu.Link,
                    MenuVisibleWithForbidden = menu.MenuVisibleWithForbidden,
                    Order = menu.OrderNo,
                    Title = menu.TitleKey,
                },
                Name = menu.Name,
                Path = menu.Path,
                Redirect = menu.Redirect,
            })
            .ToList();
    }
}
