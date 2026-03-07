using CGMSportFinance.Api.Infrastructure.Configuration;
using CGMSportFinance.Secrets;
using Microsoft.Extensions.Configuration;

namespace CGMSportFinance.Api.IntegrationTests;

public sealed class EncryptedConfigurationProviderTests : IDisposable
{
    private readonly string tempDirectory = Path.Combine(Path.GetTempPath(), $"cgmsportfinance-config-{Guid.NewGuid():N}");

    public EncryptedConfigurationProviderTests()
    {
        Directory.CreateDirectory(tempDirectory);
    }

    [Fact]
    public void EncryptedSecretsOverrideDevelopmentJson_AndCanBeOverriddenLater()
    {
        File.WriteAllText(Path.Combine(tempDirectory, "appsettings.json"), """
            {
              "ConnectionStrings": {
                "DefaultConnection": ""
              },
              "Jwt": {
                "SigningKey": ""
              }
            }
            """);

        File.WriteAllText(Path.Combine(tempDirectory, "appsettings.Development.json"), """
            {
              "ConnectionStrings": {
                "DefaultConnection": "Host=dev-json;Port=5432;Database=dev;Username=dev;Password=dev"
              },
              "Jwt": {
                "SigningKey": "from-development-json"
              }
            }
            """);

        var encryptedJson = EncryptedSecretsSerializer.EncryptJson("""
            {
              "ConnectionStrings": {
                "DefaultConnection": "Host=encrypted;Port=5432;Database=encrypted;Username=encrypted;Password=encrypted"
              },
              "Jwt": {
                "SigningKey": "from-encrypted-json"
              }
            }
            """);

        File.WriteAllText(Path.Combine(tempDirectory, "appsettings.secrets.enc.json"), encryptedJson);

        var configuration = new ConfigurationBuilder()
            .SetBasePath(tempDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEncryptedJsonFile("appsettings.secrets.enc.json", optional: false)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=override;Port=5432;Database=override;Username=override;Password=override",
                ["Jwt:SigningKey"] = "from-override",
            })
            .Build();

        Assert.Equal("Host=override;Port=5432;Database=override;Username=override;Password=override", configuration.GetConnectionString("DefaultConnection"));
        Assert.Equal("from-override", configuration["Jwt:SigningKey"]);
    }

    [Fact]
    public void EncryptedSecretsLoad_WhenNoLaterOverridesExist()
    {
        File.WriteAllText(Path.Combine(tempDirectory, "appsettings.json"), """
            {
              "ConnectionStrings": {
                "DefaultConnection": ""
              },
              "Jwt": {
                "SigningKey": ""
              }
            }
            """);

        var encryptedJson = EncryptedSecretsSerializer.EncryptJson("""
            {
              "ConnectionStrings": {
                "DefaultConnection": "Host=encrypted;Port=5432;Database=encrypted;Username=encrypted;Password=encrypted"
              },
              "Jwt": {
                "SigningKey": "from-encrypted-json"
              }
            }
            """);

        File.WriteAllText(Path.Combine(tempDirectory, "appsettings.secrets.enc.json"), encryptedJson);

        var configuration = new ConfigurationBuilder()
            .SetBasePath(tempDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddEncryptedJsonFile("appsettings.secrets.enc.json", optional: false)
            .Build();

        Assert.Equal("Host=encrypted;Port=5432;Database=encrypted;Username=encrypted;Password=encrypted", configuration.GetConnectionString("DefaultConnection"));
        Assert.Equal("from-encrypted-json", configuration["Jwt:SigningKey"]);
    }

    public void Dispose()
    {
        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }
}
