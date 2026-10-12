using Fdw.Services.Configuration;
using FdwReferenceHosts.Cors.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

using Fdw.Services.Hosts.Abstractions;
namespace FdwReferenceHosts.Cors;

/// <summary>Supplies the CorsHost implementation's own configuration.</summary>
public sealed class CorsHostConfigurationProvider
    : ImplementationProviderBase<CorsHostConfiguration, ICorsImplementationConfiguration>,
      ICorsHostConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="CorsHostConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The store this provider's rows live in -- passed by its registration.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    public CorsHostConfigurationProvider(
        ILogger<CorsHostConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
