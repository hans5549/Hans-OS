namespace CGMSportFinance.Api.Features.Profile.Contracts;

public sealed record ProfileBasicResponse(
    string Email,
    string Introduction,
    string PhoneNumber,
    string RealName,
    IReadOnlyCollection<string> Roles,
    string Username);
