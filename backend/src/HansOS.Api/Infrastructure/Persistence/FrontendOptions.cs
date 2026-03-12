namespace HansOS.Api.Infrastructure.Persistence;

public sealed class FrontendOptions
{
    public const string SectionName = "Frontend";

    public List<string> AllowedOrigins { get; init; } = [];
}
