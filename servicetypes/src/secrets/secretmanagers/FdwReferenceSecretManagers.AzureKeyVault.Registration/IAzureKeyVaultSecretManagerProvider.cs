using Fdw.Services.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;

namespace FdwReferenceSecretManagers.AzureKeyVault.Registration;

/// <summary>The AzureKeyVaultSecretManager implementation of its domain.</summary>
public interface IAzureKeyVaultSecretManagerProvider : IImplementationServiceProvider<ISecretManager, ISecretManagerImplementationConfiguration>
{
}
