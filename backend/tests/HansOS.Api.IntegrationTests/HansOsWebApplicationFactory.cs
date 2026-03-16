using HansOS.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HansOS.Api.IntegrationTests;

public class HansOsWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Remove ALL EF Core / Npgsql registrations to avoid provider conflict
            var toRemove = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                    d.ServiceType.FullName?.Contains("EntityFramework") == true ||
                    d.ServiceType.FullName?.Contains("Npgsql") == true ||
                    d.ImplementationType?.FullName?.Contains("Npgsql") == true ||
                    d.ServiceType == typeof(ApplicationDbContext))
                .ToList();

            foreach (var d in toRemove)
                services.Remove(d);

            // Re-register with InMemory provider
            var dbName = $"HansOS-Test-{Guid.NewGuid()}";
            services.AddDbContext<ApplicationDbContext>(opt =>
                opt.UseInMemoryDatabase(dbName));
        });
    }
}
