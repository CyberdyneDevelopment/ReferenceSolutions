using System.Diagnostics.CodeAnalysis;
using Azure.Core;
using Fdw.Collections;
using FdwReferenceSecretManagers.AzureKeyVault.Configuration;

namespace FdwReferenceSecretManagers.AzureKeyVault.CredentialTypes;

/// <summary>
/// Base class for Azure credential types using the CRTP pattern.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class AzureCredentialTypeBase : TypeOptionBase<AzureCredentialTypeBase>, IAzureCredentialType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzureCredentialTypeBase"/> class.
    /// </summary>
    /// <param name="name">The name for this credential type.</param>
    protected AzureCredentialTypeBase(string name) : base(name) { }

    /// <inheritdoc/>
    public abstract TokenCredential Create(AzureKeyVaultConfiguration config);
}
