using Microsoft.AspNetCore.Identity;

namespace CGMSportFinance.Api.Infrastructure.Identity;

public sealed class RoleMenu
{
    public Menu Menu { get; set; } = null!;

    public Guid MenuId { get; set; }

    public IdentityRole Role { get; set; } = null!;

    public string RoleId { get; set; } = string.Empty;
}
