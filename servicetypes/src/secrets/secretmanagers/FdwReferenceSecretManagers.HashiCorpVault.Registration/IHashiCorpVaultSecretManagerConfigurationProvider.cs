using Fdw.Services.Abstractions;

using Fdw.Services.SecretManagers.Abstractions;
namespace FdwReferenceSecretManagers.HashiCorpVault.Registration;

/// <summary>Supplies the HashiCorpVaultSecretManager implementation's own configuration.</summary>
public interface IHashiCorpVaultSecretManagerConfigurationProvider
    : IImplementationConfigurationProvider<ISecretManagerImplementationConfiguration>
{
}
