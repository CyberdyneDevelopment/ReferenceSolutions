using Fdw.Services.Configuration;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.MsSql;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace FdwReferenceConnections.MsSql;

/// <summary>Supplies the MsSqlConnection implementation's own configuration.</summary>
public sealed class MsSqlConnectionConfigurationProvider
    : ImplementationProviderBase<MsSqlConnectionConfiguration, IConnectionImplementationConfiguration>,
      IMsSqlConnectionConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="MsSqlConnectionConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The store this provider's rows live in -- passed by its registration.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    public MsSqlConnectionConfigurationProvider(
        ILogger<MsSqlConnectionConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
