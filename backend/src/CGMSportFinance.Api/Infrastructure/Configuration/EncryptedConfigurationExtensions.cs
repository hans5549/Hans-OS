using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;

namespace CGMSportFinance.Api.Infrastructure.Configuration;

public static class EncryptedConfigurationExtensions
{
    public static IConfigurationBuilder AddEncryptedJsonFile(
        this IConfigurationBuilder configuration,
        string path,
        bool optional)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var source = new EncryptedConfigurationSource
        {
            Optional = optional,
            Path = path,
            ReloadOnChange = false,
        };

        configuration.Add(source);
        return configuration;
    }

    public static ConfigurationManager AddEncryptedJsonFileBeforeEnvironmentVariables(
        this ConfigurationManager configuration,
        string path,
        bool optional)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var source = CreateSource(path, optional);

        var insertIndex = configuration.Sources
            .Select((item, index) => new { item, index })
            .FirstOrDefault(entry => entry.item is EnvironmentVariablesConfigurationSource)?
            .index ?? configuration.Sources.Count;

        configuration.Sources.Insert(insertIndex, source);
        return configuration;
    }

    private static EncryptedConfigurationSource CreateSource(string path, bool optional) =>
        new()
        {
            Optional = optional,
            Path = path,
            ReloadOnChange = false,
        };
}
