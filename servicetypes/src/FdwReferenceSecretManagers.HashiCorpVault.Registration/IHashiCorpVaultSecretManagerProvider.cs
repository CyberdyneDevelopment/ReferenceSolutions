using Fdw.Services.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;

namespace FdwReferenceSecretManagers.HashiCorpVault.Registration;

/// <summary>The HashiCorpVaultSecretManager implementation of its domain.</summary>
public interface IHashiCorpVaultSecretManagerProvider : IImplementationServiceProvider<ISecretManager, ISecretManagerImplementationConfiguration>
{
}
