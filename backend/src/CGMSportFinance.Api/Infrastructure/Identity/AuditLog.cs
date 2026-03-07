namespace CGMSportFinance.Api.Infrastructure.Identity;

public sealed class AuditLog
{
    public string? Action { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public string? DetailsJson { get; set; }

    public long Id { get; set; }

    public string? IpAddress { get; set; }

    public string? ResourceId { get; set; }

    public string? ResourceType { get; set; }

    public string? UserId { get; set; }
}
