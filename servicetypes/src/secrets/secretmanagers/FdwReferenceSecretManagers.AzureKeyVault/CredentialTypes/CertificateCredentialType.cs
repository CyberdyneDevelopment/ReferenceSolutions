using System;
using System.Diagnostics.CodeAnalysis;
using Azure.Core;
using Azure.Identity;
using Fdw.Collections.Attributes;
using FdwReferenceSecretManagers.AzureKeyVault.Configuration;

namespace FdwReferenceSecretManagers.AzureKeyVault.CredentialTypes;

/// <summary>
/// Azure credential type that uses certificate-based authentication.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(AzureCredentialTypes), "Certificate")]
public sealed class CertificateCredentialType : AzureCredentialTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CertificateCredentialType"/> class.
    /// </summary>
    public CertificateCredentialType() : base("Certificate") { }

    /// <inheritdoc/>
    public override TokenCredential Create(AzureKeyVaultConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(config.TenantId) ||
            string.IsNullOrWhiteSpace(config.ClientId) ||
            string.IsNullOrWhiteSpace(config.CertificatePath))
        {
            throw new InvalidOperationException(
                "TenantId, ClientId, and CertificatePath are required for Certificate authentication");
        }

        return new ClientCertificateCredential(
            config.TenantId,
            config.ClientId,
            config.CertificatePath);
    }
}
