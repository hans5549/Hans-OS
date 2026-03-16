namespace HansOS.Api.Data.Entities;

public class RoleMenu
{
    public string RoleId { get; set; } = string.Empty;
    public Guid MenuId { get; set; }

    public Menu Menu { get; set; } = null!;
}
