using Fdw.Services.Configuration;
using Fdw.Services.DataVault;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

using Fdw.Services.DataVault.Abstractions;
namespace FdwReferenceConnections.MsSql.DataVault.Registration;

/// <summary>Supplies the DefaultDataVault implementation's own configuration.</summary>
public sealed class DefaultDataVaultConfigurationProvider
    : ImplementationProviderBase<SqlDataVaultConfiguration, IDataVaultImplementationConfiguration>,
      IDefaultDataVaultConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="DefaultDataVaultConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The store this provider's rows live in -- passed by its registration.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    public DefaultDataVaultConfigurationProvider(
        ILogger<DefaultDataVaultConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
