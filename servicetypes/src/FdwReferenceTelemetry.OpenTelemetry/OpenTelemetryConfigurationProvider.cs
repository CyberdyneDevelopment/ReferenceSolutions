using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Telemetry.Abstractions;
using Microsoft.Extensions.Logging;

namespace FdwReferenceTelemetry.OpenTelemetry;

/// <summary>Supplies the OpenTelemetry implementation's own configuration.</summary>
public sealed class OpenTelemetryConfigurationProvider
    : ImplementationProviderBase<OpenTelemetryConfiguration, ITelemetryImplementationConfiguration>,
      IOpenTelemetryConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="OpenTelemetryConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The store this provider's rows live in -- passed by its registration.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    public OpenTelemetryConfigurationProvider(
        ILogger<OpenTelemetryConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
