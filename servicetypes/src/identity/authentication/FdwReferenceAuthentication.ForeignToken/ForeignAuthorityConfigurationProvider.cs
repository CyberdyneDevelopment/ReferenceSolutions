using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuthentication.ForeignToken;

/// <summary>Supplies the configured ForeignAuthority members.</summary>
public sealed class ForeignAuthorityConfigurationProvider
    : DomainConfigurationProviderBase<IForeignAuthorityImplementationConfiguration>,
      IForeignAuthorityConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="ForeignAuthorityConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The store this provider's rows live in -- passed by its registration.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    public ForeignAuthorityConfigurationProvider(
        ILogger<ForeignAuthorityConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
