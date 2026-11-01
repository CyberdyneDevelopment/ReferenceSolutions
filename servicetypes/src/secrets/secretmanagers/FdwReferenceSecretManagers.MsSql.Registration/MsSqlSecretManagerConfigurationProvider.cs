using Fdw.Services.Configuration;
using FdwReferenceSecretManagers.MsSql.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

using Fdw.Services.SecretManagers.Abstractions;
namespace FdwReferenceSecretManagers.MsSql.Registration;

/// <summary>Supplies the MsSqlSecretManager implementation's own configuration.</summary>
public sealed class MsSqlSecretManagerConfigurationProvider
    : ImplementationProviderBase<MsSqlSecretManagerConfiguration, ISecretManagerImplementationConfiguration>,
      IMsSqlSecretManagerConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="MsSqlSecretManagerConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The store this provider's rows live in -- passed by its registration.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    public MsSqlSecretManagerConfigurationProvider(
        ILogger<MsSqlSecretManagerConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
