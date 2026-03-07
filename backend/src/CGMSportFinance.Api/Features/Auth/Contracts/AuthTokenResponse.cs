namespace CGMSportFinance.Api.Features.Auth.Contracts;

public sealed record AuthTokenResponse(string AccessToken, int ExpiresIn);
