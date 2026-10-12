using System.Diagnostics.CodeAnalysis;
using Azure.Core;
using Azure.Identity;
using Fdw.Collections.Attributes;
using FdwReferenceSecretManagers.AzureKeyVault.Configuration;

namespace FdwReferenceSecretManagers.AzureKeyVault.CredentialTypes;

/// <summary>
/// Azure credential type that uses device code authentication.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(AzureCredentialTypes), "DeviceCode")]
public sealed class DeviceCodeCredentialType : AzureCredentialTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceCodeCredentialType"/> class.
    /// </summary>
    public DeviceCodeCredentialType() : base("DeviceCode") { }

    /// <inheritdoc/>
    public override TokenCredential Create(AzureKeyVaultConfiguration config)
    {
        return new DeviceCodeCredential();
    }
}
