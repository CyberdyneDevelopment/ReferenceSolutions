using Azure.Core;
using Fdw.Collections;
using FdwReferenceSecretManagers.AzureKeyVault.Configuration;

namespace FdwReferenceSecretManagers.AzureKeyVault.CredentialTypes;

/// <summary>
/// Defines a credential type for Azure Key Vault authentication.
/// </summary>
public interface IAzureCredentialType : ITypeOption<int, AzureCredentialTypeBase>
{
    /// <summary>
    /// Creates the Azure <see cref="TokenCredential"/> for this authentication method.
    /// </summary>
    /// <param name="config">The Azure Key Vault configuration.</param>
    /// <returns>A configured <see cref="TokenCredential"/> instance.</returns>
    TokenCredential Create(AzureKeyVaultConfiguration config);
}
