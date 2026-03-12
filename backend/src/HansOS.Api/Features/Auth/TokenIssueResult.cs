using HansOS.Api.Infrastructure.Identity;

namespace HansOS.Api.Features.Auth;

public sealed record RefreshTokenIssue(string PlainTextToken, RefreshToken RefreshToken);

public sealed record TokenIssueResult(string AccessToken, int ExpiresIn);
