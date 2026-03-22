using System.Text;
using HansOS.Api.Data;
using HansOS.Api.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace HansOS.Api.IntegrationTests;

public class HansOsWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string TestSigningKey =
        "integration-test-signing-key-must-be-at-least-32-bytes-long";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = TestSigningKey,
                ["Jwt:Issuer"] = "HansOS",
                ["Jwt:Audience"] = "HansOS",
            });
        });

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

            // Override JWT options for AuthService (token signing)
            services.Configure<JwtOptions>(opt =>
            {
                opt.SigningKey = TestSigningKey;
                opt.Issuer = "HansOS";
                opt.Audience = "HansOS";
            });

            // Override JWT bearer middleware (token validation) —
            // Program.cs captures jwtOptions at startup via closure,
            // so PostConfigure is needed to align the validation key.
            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                opt =>
                {
                    opt.TokenValidationParameters.IssuerSigningKey =
                        new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(TestSigningKey));
                    opt.TokenValidationParameters.ValidIssuer = "HansOS";
                    opt.TokenValidationParameters.ValidAudience = "HansOS";
                });
        });
    }
}
