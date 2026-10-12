using Fdw.Services.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;

namespace FdwReferenceSecretManagers.UserSecrets.Registration;

/// <summary>The UserSecretsSecretManager implementation of its domain.</summary>
public interface IUserSecretsSecretManagerProvider : IImplementationServiceProvider<ISecretManager, ISecretManagerImplementationConfiguration>
{
}
