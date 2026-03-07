using Microsoft.Extensions.Configuration;

namespace CGMSportFinance.Api.Infrastructure.Configuration;

public sealed class EncryptedConfigurationSource : FileConfigurationSource
{
    public override IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        EnsureDefaults(builder);
        return new EncryptedConfigurationProvider(this);
    }
}
