using System.Diagnostics.CodeAnalysis;
using Azure.Core;
using Azure.Identity;
using Fdw.Collections.Attributes;
using FdwReferenceSecretManagers.AzureKeyVault.Configuration;

namespace FdwReferenceSecretManagers.AzureKeyVault.CredentialTypes;

/// <summary>
/// Azure credential type that uses managed identity authentication.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(AzureCredentialTypes), "ManagedIdentity")]
public sealed class ManagedIdentityCredentialType : AzureCredentialTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedIdentityCredentialType"/> class.
    /// </summary>
    public ManagedIdentityCredentialType() : base("ManagedIdentity") { }

    /// <inheritdoc/>
    public override TokenCredential Create(AzureKeyVaultConfiguration config)
    {
        if (!string.IsNullOrWhiteSpace(config.ManagedIdentityId))
        {
            return new ManagedIdentityCredential(ManagedIdentityId.FromUserAssignedClientId(config.ManagedIdentityId));
        }

        return new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned);
    }
}
