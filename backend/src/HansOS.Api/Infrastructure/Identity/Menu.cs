namespace HansOS.Api.Infrastructure.Identity;

public sealed class Menu
{
    public string? ActivePath { get; set; }

    public bool AffixTab { get; set; }

    public string? Badge { get; set; }

    public string? BadgeType { get; set; }

    public string? BadgeVariant { get; set; }

    public ICollection<Menu> Children { get; set; } = [];

    public string? ComponentKey { get; set; }

    public string? Icon { get; set; }

    public Guid Id { get; set; }

    public string? IframeSrc { get; set; }

    public bool KeepAlive { get; set; }

    public string? Link { get; set; }

    public bool HideInBreadcrumb { get; set; }

    public bool HideInMenu { get; set; }

    public bool HideInTab { get; set; }

    public bool MenuVisibleWithForbidden { get; set; }

    public string Name { get; set; } = string.Empty;

    public int OrderNo { get; set; }

    public Menu? Parent { get; set; }

    public Guid? ParentId { get; set; }

    public string Path { get; set; } = string.Empty;

    public string? PermissionCode { get; set; }

    public string? Redirect { get; set; }

    public ICollection<RoleMenu> RoleMenus { get; set; } = [];

    public bool Status { get; set; } = true;

    public string TitleKey { get; set; } = string.Empty;

    public MenuType Type { get; set; } = MenuType.Menu;
}
