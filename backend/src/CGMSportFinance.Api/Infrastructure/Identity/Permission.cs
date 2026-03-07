namespace CGMSportFinance.Api.Infrastructure.Identity;

public sealed class Permission
{
    public string Code { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int Id { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
