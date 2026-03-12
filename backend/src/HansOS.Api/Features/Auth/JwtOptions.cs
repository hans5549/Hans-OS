namespace HansOS.Api.Features.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public int AccessTokenMinutes { get; init; } = 15;

    public string Audience { get; init; } = "HansOS.Frontend";

    public string Issuer { get; init; } = "HansOS.Api";

    public int RefreshTokenDays { get; init; } = 14;

    public string SigningKey { get; init; } = string.Empty;
}
