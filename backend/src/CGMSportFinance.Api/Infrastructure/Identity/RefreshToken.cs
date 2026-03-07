namespace CGMSportFinance.Api.Infrastructure.Identity;

public sealed class RefreshToken
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public string? CreatedByIp { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public Guid Id { get; set; }

    public bool IsActive => RevokedAt is null && ExpiresAt > DateTimeOffset.UtcNow;

    public Guid? ReplacedByTokenId { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public string? RevokedByIp { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public string UserAgent { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;
}
