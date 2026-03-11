namespace CGMSportFinance.Api.Features.Profile.Contracts;

public sealed record ProfileHeaderResponse(
    string Avatar,
    string RealName,
    IReadOnlyCollection<string> Roles,
    string UserId,
    string Username);
