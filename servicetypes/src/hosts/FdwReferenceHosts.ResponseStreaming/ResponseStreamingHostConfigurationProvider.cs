using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Hosts.Abstractions;
using Microsoft.Extensions.Logging;
using FdwReferenceHosts.ResponseStreaming.Configuration;

namespace FdwReferenceHosts.ResponseStreaming;

/// <summary>Supplies the ResponseStreamingHost implementation's own configuration.</summary>
public sealed class ResponseStreamingHostConfigurationProvider
    : ImplementationProviderBase<ResponseStreamingHostConfiguration, IResponseStreamingImplementationConfiguration>,
      IResponseStreamingHostConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="ResponseStreamingHostConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The store this implementation's rows live in -- the one its domain reads.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    public ResponseStreamingHostConfigurationProvider(
        ILogger<ResponseStreamingHostConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
