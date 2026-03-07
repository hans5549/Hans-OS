using CGMSportFinance.Api.Infrastructure.Identity;

namespace CGMSportFinance.Api.Features.Auth;

public sealed record RefreshTokenIssue(string PlainTextToken, RefreshToken RefreshToken);

public sealed record TokenIssueResult(string AccessToken, int ExpiresIn);
