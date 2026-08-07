using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Certificates;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Abstractions.Handlers;
using Fdw.Services.SecretManagers.Abstractions.Results;
using ReferenceSecretManagers.AzureKeyVault.Logging;
using Fdw.Services.SecretManagers.Commands;
using Fdw.Services.SecretManagers.Handlers;
using ReferenceSecretManagers.AzureKeyVault.Commands;
using Fdw.Services.SecretManagers.AzureKeyVault.Configuration;
using Fdw.Services.SecretManagers.AzureKeyVault.CredentialTypes;
using Fdw.Services.SecretManagers.AzureKeyVault.Commands;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.AzureKeyVault.Handlers;

/// <summary>
/// Handler for GetCertificate commands against Azure Key Vault.
/// Retrieves certificates as binary data (PFX format) for use in WS-Security, TLS, etc.
/// </summary>
[TypeOption(typeof(AzureKeyVaultCommandHandlers), "GetCertificate")]
[ExcludeFromCodeCoverage(Justification = "TypeOption handler - integration tested")]
public sealed class GetCertificateCommandHandler
    : SecretManagerCommandHandlerBase<GetCertificateManagerCommand, SecretValue>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetCertificateCommandHandler"/> class.
    /// </summary>
    public GetCertificateCommandHandler()
        : base(id: 5, name: "GetCertificate")
    {
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<SecretValue>> Execute(
        GetCertificateManagerCommand command,
        ISecretManagerExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (context is not AzureKeyVaultExecutionContext akvContext)
        {
            return GenericResult<SecretValue>.Failure(
                SecretManagerResultCodes.ByName("InvalidExecutionContext"),
                ResultDetails.Create().With("ExpectedType", nameof(AzureKeyVaultExecutionContext)));
        }

        if (akvContext.CertificateClient is null)
        {
            return GenericResult<SecretValue>.Failure(
                AzureKeyVaultLogger.CertificateClientNotInitialized(akvContext.Logger));
        }

        if (string.IsNullOrWhiteSpace(command.SecretKey))
        {
            return GenericResult<SecretValue>.Failure(
                AzureKeyVaultLogger.SecretKeyRequired(akvContext.Logger, "GetCertificate"));
        }

        try
        {
            var certificateName = command.SecretKey;
            var response = await akvContext.CertificateClient
                .DownloadCertificateAsync(certificateName, command.Version, cancellationToken)
                .ConfigureAwait(false);

            return BuildSecretValue(response.Value, certificateName, command);
        }
        catch (Exception ex)
        {
            return GenericResult<SecretValue>.Failure(
                AzureKeyVaultLogger.OperationFailed(akvContext.Logger, "GetCertificate", command.SecretKey, ex.Message));
        }
    }

    private static IGenericResult<SecretValue> BuildSecretValue(
        X509Certificate2 certificate,
        string certificateName,
        GetCertificateManagerCommand command)
    {
        var exportFormat = command.IncludePrivateKey
            ? X509ContentType.Pfx
            : X509ContentType.Cert;

        byte[] certBytes;
        try
        {
            certBytes = certificate.Export(exportFormat);
        }
        catch (Exception ex)
        {
            return GenericResult<SecretValue>.Failure(
                SecretManagerResultCodes.ByName("CertificateExportFailed"),
                ResultDetails.Create()
                    .With("CertificateName", certificateName)
                    .With("ErrorMessage", ex.Message));
        }

        var metadata = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["Thumbprint"] = certificate.Thumbprint ?? string.Empty,
            ["SubjectName"] = certificate.SubjectName.Name ?? string.Empty,
            ["IssuerName"] = certificate.IssuerName.Name ?? string.Empty,
            ["HasPrivateKey"] = certificate.HasPrivateKey,
            ["NotBefore"] = certificate.NotBefore,
            ["NotAfter"] = certificate.NotAfter
        };

        var secretValue = new SecretValue(
            key: certificateName,
            value: certBytes,
            version: command.Version,
            createdAt: new DateTimeOffset(certificate.NotBefore),
            modifiedAt: new DateTimeOffset(certificate.NotBefore),
            expiresAt: new DateTimeOffset(certificate.NotAfter),
            metadata: metadata);

        return GenericResult<SecretValue>.Success(secretValue);
    }

    /// <inheritdoc />
    protected override IGenericResult ValidateTypedCommand(GetCertificateManagerCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.SecretKey))
        {
            return GenericResult.Failure(
                SecretManagerResultCodes.ByName("CertificateNameRequired"),
                ResultDetails.Create().With("Operation", "GetCertificate"));
        }

        return GenericResult.Success();
    }
}
