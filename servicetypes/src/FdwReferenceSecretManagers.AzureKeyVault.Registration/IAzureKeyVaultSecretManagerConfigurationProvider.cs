using Fdw.Services.Abstractions;

using Fdw.Services.SecretManagers.Abstractions;
namespace FdwReferenceSecretManagers.AzureKeyVault.Registration;

/// <summary>Supplies the AzureKeyVaultSecretManager implementation's own configuration.</summary>
public interface IAzureKeyVaultSecretManagerConfigurationProvider
    : IImplementationConfigurationProvider<ISecretManagerImplementationConfiguration>
{
}
