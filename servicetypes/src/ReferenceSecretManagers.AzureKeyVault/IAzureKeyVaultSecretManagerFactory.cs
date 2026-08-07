using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.AzureKeyVault.Configuration;
using ReferenceSecretManagers.AzureKeyVault.Commands;
using Fdw.Services.SecretManagers.AzureKeyVault.CredentialTypes;
using Fdw.Services.SecretManagers.AzureKeyVault.Commands;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.AzureKeyVault;

/// <summary>
/// Factory interface for creating Azure Key Vault secret manager service instances.
/// </summary>
/// <remarks>
/// Registered as singleton in DI (Phase 1) and resolved in Phase 2.
/// Follows the ServiceType pattern where dependencies are injected via constructor
/// and configuration is passed to the Get method at runtime.
/// </remarks>
public interface IAzureKeyVaultSecretManagerFactory : ISecretManagerServiceFactory<ISecretManager, AzureKeyVaultConfiguration>
{
    // Inherits Get(AzureKeyVaultConfiguration) from base interface
}
