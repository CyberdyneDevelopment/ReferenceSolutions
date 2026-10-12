using Fdw.Services.Configuration;
using FdwReferenceSecretManagers.EnvironmentVariable.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

using Fdw.Services.SecretManagers.Abstractions;
namespace FdwReferenceSecretManagers.EnvironmentVariable.Registration;

/// <summary>Supplies the EnvironmentVariableSecretManager implementation's own configuration.</summary>
public sealed class EnvironmentVariableSecretManagerConfigurationProvider
    : ImplementationProviderBase<EnvironmentVariableConfiguration, ISecretManagerImplementationConfiguration>,
      IEnvironmentVariableSecretManagerConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="EnvironmentVariableSecretManagerConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The store this provider's rows live in -- passed by its registration.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    public EnvironmentVariableSecretManagerConfigurationProvider(
        ILogger<EnvironmentVariableSecretManagerConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
