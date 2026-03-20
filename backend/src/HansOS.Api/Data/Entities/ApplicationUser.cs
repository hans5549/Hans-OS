using Microsoft.AspNetCore.Identity;

namespace HansOS.Api.Data.Entities;

public class ApplicationUser : IdentityUser
{
    public string? Avatar { get; set; }
    public string? Desc { get; set; }
    public string? HomePath { get; set; }
    public string RealName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
