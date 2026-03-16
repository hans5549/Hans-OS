namespace HansOS.Api.Data.Entities;

public class Menu
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }

    // Route fields
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? Component { get; set; }
    public string? Redirect { get; set; }

    // Meta fields (Vben Admin)
    public string Title { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? ActiveIcon { get; set; }
    public int Order { get; set; }
    public bool AffixTab { get; set; }
    public int? AffixTabOrder { get; set; }
    public bool HideInMenu { get; set; }
    public bool HideInTab { get; set; }
    public bool HideInBreadcrumb { get; set; }
    public bool HideChildrenInMenu { get; set; }
    public bool KeepAlive { get; set; }
    public string? Authority { get; set; } // JSON array string, e.g. ["admin"]
    public string? Badge { get; set; }
    public string? BadgeType { get; set; }
    public string? BadgeVariants { get; set; }
    public string? Link { get; set; }
    public string? IframeSrc { get; set; }
    public bool NoBasicLayout { get; set; }
    public int? MaxNumOfOpenTab { get; set; }

    public MenuType Type { get; set; } = MenuType.Menu;
    public bool IsActive { get; set; } = true;

    // Navigation
    public Menu? Parent { get; set; }
    public ICollection<Menu> Children { get; set; } = [];
    public ICollection<RoleMenu> RoleMenus { get; set; } = [];
}
