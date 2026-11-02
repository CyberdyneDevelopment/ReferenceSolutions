using Fdw.Services.Configuration;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Http;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace FdwReferenceConnections.Http;

/// <summary>Supplies the HttpConnection implementation's own configuration.</summary>
public sealed class HttpConnectionConfigurationProvider
    : ImplementationProviderBase<HttpConnectionConfiguration, IConnectionImplementationConfiguration>,
      IHttpConnectionConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="HttpConnectionConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The store this provider's rows live in -- passed by its registration.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    public HttpConnectionConfigurationProvider(
        ILogger<HttpConnectionConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
