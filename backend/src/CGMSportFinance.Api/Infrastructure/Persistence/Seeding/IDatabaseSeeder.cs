namespace CGMSportFinance.Api.Infrastructure.Persistence.Seeding;

public interface IDatabaseSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
