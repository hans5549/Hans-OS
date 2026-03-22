using System.Security.Claims;
using HansOS.Api.Common;
using HansOS.Api.Models.Menus;
using HansOS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HansOS.Api.Controllers;

[ApiController]
[Route("menu")]
[Authorize]
public class MenuController(IMenuService menuService) : ControllerBase
{
    [HttpGet("all")]
    public async Task<ApiEnvelope<List<MenuRouteResponse>>> GetAll(CancellationToken ct)
    {
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var menus = await menuService.GetMenusByRolesAsync(roles, ct);
        return ApiEnvelope<List<MenuRouteResponse>>.Success(menus);
    }
}
