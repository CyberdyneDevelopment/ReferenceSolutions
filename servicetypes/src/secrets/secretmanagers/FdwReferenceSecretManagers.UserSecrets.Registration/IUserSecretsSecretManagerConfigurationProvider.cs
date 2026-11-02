using Fdw.Services.Abstractions;

using Fdw.Services.SecretManagers.Abstractions;
namespace FdwReferenceSecretManagers.UserSecrets.Registration;

/// <summary>Supplies the UserSecretsSecretManager implementation's own configuration.</summary>
public interface IUserSecretsSecretManagerConfigurationProvider
    : IImplementationConfigurationProvider<ISecretManagerImplementationConfiguration>
{
}
