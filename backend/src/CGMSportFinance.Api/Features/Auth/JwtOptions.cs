namespace CGMSportFinance.Api.Features.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public int AccessTokenMinutes { get; init; } = 15;

    public string Audience { get; init; } = "CGMSportFinance.Frontend";

    public string Issuer { get; init; } = "CGMSportFinance.Api";

    public int RefreshTokenDays { get; init; } = 14;

    public string SigningKey { get; init; } = string.Empty;
}
