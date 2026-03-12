using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace HansOS.Api.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser
{
    [MaxLength(512)]
    public string Avatar { get; set; } = string.Empty;

    [MaxLength(256)]
    public string HomePath { get; set; } = "/analytics";

    public bool IsActive { get; set; } = true;

    [MaxLength(120)]
    public string RealName { get; set; } = string.Empty;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
