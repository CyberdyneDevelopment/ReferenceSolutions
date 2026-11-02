using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuthentication.OidcRedirect;

/// <summary>Supplies the OidcAuthority implementation's own configuration.</summary>
public sealed class OidcAuthorityImplementationConfigurationProvider
    : ImplementationProviderBase<OidcAuthorityConfiguration, IOidcAuthorityImplementationConfiguration>,
      IOidcAuthorityImplementationConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="OidcAuthorityImplementationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The store this provider's rows live in -- passed by its registration.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    public OidcAuthorityImplementationConfigurationProvider(
        ILogger<OidcAuthorityImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
