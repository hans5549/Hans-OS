namespace CGMSportFinance.Api.Infrastructure.Persistence.Seeding;

public sealed class SeedingOptions
{
    public const string SectionName = "Seeding";

    public bool EnableDemoData { get; init; }
}
