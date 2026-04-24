using System.ComponentModel.DataAnnotations;

namespace HansOS.Api.Options;

public class IdentitySeedOptions
{
    public const string SectionName = "IdentitySeed";

    [Required] public string AdminRoleName { get; set; } = "admin";
    [Required] public string AdminUserName { get; set; } = "hans";
    [Required] public string AdminRealName { get; set; } = "Hans";
    [Required, EmailAddress] public string AdminEmail { get; set; } = "hans@hans-os.dev";
    [Required] public string AdminHomePath { get; set; } = "/index";
    public string AdminPassword { get; set; } = string.Empty;
}
