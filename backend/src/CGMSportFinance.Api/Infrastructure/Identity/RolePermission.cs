using Microsoft.AspNetCore.Identity;

namespace CGMSportFinance.Api.Infrastructure.Identity;

public sealed class RolePermission
{
    public Permission Permission { get; set; } = null!;

    public int PermissionId { get; set; }

    public IdentityRole Role { get; set; } = null!;

    public string RoleId { get; set; } = string.Empty;
}
