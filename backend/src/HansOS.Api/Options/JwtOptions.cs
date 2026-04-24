using System.ComponentModel.DataAnnotations;

namespace HansOS.Api.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public const string DevelopmentSigningKey = "hans-os-default-jwt-signing-key-must-be-at-least-32-bytes-long";

    [Required] public string Issuer { get; set; } = string.Empty;
    [Required] public string Audience { get; set; } = string.Empty;
    [Required, MinLength(32)] public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenExpiryMinutes { get; set; } = 30;
    public int RefreshTokenExpiryDays { get; set; } = 7;
}
