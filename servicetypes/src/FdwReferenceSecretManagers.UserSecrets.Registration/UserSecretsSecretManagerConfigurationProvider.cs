using Fdw.Services.Configuration;
using FdwReferenceSecretManagers.UserSecrets.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

using Fdw.Services.SecretManagers.Abstractions;
namespace FdwReferenceSecretManagers.UserSecrets.Registration;

/// <summary>Supplies the UserSecretsSecretManager implementation's own configuration.</summary>
public sealed class UserSecretsSecretManagerConfigurationProvider
    : ImplementationProviderBase<UserSecretsConfiguration, ISecretManagerImplementationConfiguration>,
      IUserSecretsSecretManagerConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="UserSecretsSecretManagerConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The store this provider's rows live in -- passed by its registration.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    public UserSecretsSecretManagerConfigurationProvider(
        ILogger<UserSecretsSecretManagerConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
