namespace CGMSportFinance.Api.Features.Profile.Contracts;

public sealed record ProfileSecurityResponse(
    bool HasEmail,
    bool HasPassword,
    bool HasPhoneNumber,
    bool TwoFactorEnabled);
