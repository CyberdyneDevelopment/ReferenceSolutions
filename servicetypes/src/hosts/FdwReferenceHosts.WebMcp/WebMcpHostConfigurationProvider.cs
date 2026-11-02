using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Hosts.Abstractions;
using Microsoft.Extensions.Logging;
using FdwReferenceHosts.WebMcp.Configuration;

namespace FdwReferenceHosts.WebMcp;

/// <summary>Supplies the WebMcpHost implementation's own configuration.</summary>
public sealed class WebMcpHostConfigurationProvider
    : ImplementationProviderBase<WebMcpHostConfiguration, IWebMcpImplementationConfiguration>,
      IWebMcpHostConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="WebMcpHostConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The store this implementation's rows live in -- the one its domain reads.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    public WebMcpHostConfigurationProvider(
        ILogger<WebMcpHostConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
