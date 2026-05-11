using HansOS.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.Data.Seeding;

public sealed class DatabaseStartupService(
    IServiceProvider services,
    IConfiguration configuration,
    ILogger<DatabaseStartupService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (configuration.GetValue<bool>("DatabaseStartup:RunBeforeListen"))
        {
            await RunStartupTasksAsync(cancellationToken, rethrow: true);
            return;
        }

        _ = Task.Run(
            () => RunStartupTasksAsync(CancellationToken.None, rethrow: false),
            CancellationToken.None);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task RunStartupTasksAsync(CancellationToken cancellationToken, bool rethrow)
    {
        try
        {
            logger.LogInformation("Starting database migration and identity seed.");
            using (var scope = services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                if (db.Database.IsRelational())
                    await db.Database.MigrateAsync(cancellationToken);
            }

            await IdentitySeeder.SeedAsync(services, cancellationToken);
            logger.LogInformation("Database migration and identity seed completed.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Database startup task was canceled.");
            if (rethrow)
                throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database migration or identity seed failed during startup.");
            if (rethrow)
                throw;
        }
    }
}
