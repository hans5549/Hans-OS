using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CGMSportFinance.Api.IntegrationTests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string sqlitePath = Path.Combine(Path.GetTempPath(), $"cgmsportfinance-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Database:Provider", "Sqlite");
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={sqlitePath}");
        builder.UseSetting("Frontend:AllowedOrigins:0", "http://localhost:5666");
        builder.UseSetting("Jwt:SigningKey", "integration-tests-signing-key-32chars-minimum");
    }
}
