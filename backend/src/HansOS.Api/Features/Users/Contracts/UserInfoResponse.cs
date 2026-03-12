namespace HansOS.Api.Features.Users.Contracts;

public sealed record UserInfoResponse(
    string Avatar,
    string HomePath,
    string RealName,
    IReadOnlyCollection<string> Roles,
    string UserId,
    string Username);
