using HansOS.Api.Models.Menus;

namespace HansOS.Api.Services;

public interface IMenuService
{
    Task<List<MenuRouteResponse>> GetMenusByRolesAsync(IList<string> roles, CancellationToken ct = default);
}
