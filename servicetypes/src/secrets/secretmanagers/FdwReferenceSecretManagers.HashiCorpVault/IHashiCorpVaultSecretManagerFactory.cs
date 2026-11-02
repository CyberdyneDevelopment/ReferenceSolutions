using Fdw.Services.SecretManagers.Abstractions;
using FdwReferenceSecretManagers.HashiCorpVault.Configuration;

namespace FdwReferenceSecretManagers.HashiCorpVault;

/// <summary>Builds the HashiCorp Vault secret manager from its configuration.</summary>
public interface IHashiCorpVaultSecretManagerFactory : ISecretManagerServiceFactory<ISecretManager, HashiCorpVaultConfiguration>
{
}
