using System;
using System.Diagnostics.CodeAnalysis;
using Azure.Core;
using Azure.Identity;
using Fdw.Collections.Attributes;
using FdwReferenceSecretManagers.AzureKeyVault.Configuration;

namespace FdwReferenceSecretManagers.AzureKeyVault.CredentialTypes;

/// <summary>
/// Azure credential type that uses service principal (client secret) authentication.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(AzureCredentialTypes), "ServicePrincipal")]
public sealed class ServicePrincipalCredentialType : AzureCredentialTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServicePrincipalCredentialType"/> class.
    /// </summary>
    public ServicePrincipalCredentialType() : base("ServicePrincipal") { }

    /// <inheritdoc/>
    public override TokenCredential Create(AzureKeyVaultConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(config.TenantId) ||
            string.IsNullOrWhiteSpace(config.ClientId) ||
            string.IsNullOrWhiteSpace(config.ClientSecret))
        {
            throw new InvalidOperationException(
                "TenantId, ClientId, and ClientSecret are required for ServicePrincipal authentication");
        }

        return new ClientSecretCredential(
            config.TenantId,
            config.ClientId,
            config.ClientSecret);
    }
}
