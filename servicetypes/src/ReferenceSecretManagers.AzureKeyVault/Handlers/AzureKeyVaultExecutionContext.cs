using System;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using Fdw.Configuration;
using Fdw.Services.SecretManagers.Abstractions.Handlers;
using Fdw.Services.SecretManagers.AzureKeyVault.Configuration;
using Microsoft.Extensions.Logging;
using ReferenceSecretManagers.AzureKeyVault.Commands;
using Fdw.Services.SecretManagers.AzureKeyVault.CredentialTypes;
using Fdw.Services.SecretManagers.AzureKeyVault.Commands;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.AzureKeyVault.Handlers;

/// <summary>
/// Azure Key Vault specific execution context providing access to the SecretClient and CertificateClient.
/// </summary>
public sealed class AzureKeyVaultExecutionContext : ISecretManagerExecutionContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzureKeyVaultExecutionContext"/> class.
    /// </summary>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <param name="configuration">The Azure Key Vault configuration.</param>
    /// <param name="secretClient">The Azure Key Vault secret client.</param>
    /// <param name="certificateClient">The Azure Key Vault certificate client.</param>
    /// <param name="serviceId">The service identifier.</param>
    public AzureKeyVaultExecutionContext(
        ILogger logger,
        AzureKeyVaultConfiguration configuration,
        SecretClient secretClient,
        CertificateClient? certificateClient,
        string serviceId)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        AzureConfiguration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        SecretClient = secretClient ?? throw new ArgumentNullException(nameof(secretClient));
        CertificateClient = certificateClient;
        ServiceId = serviceId ?? throw new ArgumentNullException(nameof(serviceId));
    }

    /// <inheritdoc />
    public ILogger Logger { get; }

    /// <inheritdoc />
    public IGenericConfiguration Configuration => AzureConfiguration;

    /// <summary>
    /// Gets the Azure Key Vault specific configuration.
    /// </summary>
    public AzureKeyVaultConfiguration AzureConfiguration { get; }

    /// <summary>
    /// Gets the Azure Key Vault secret client.
    /// </summary>
    public SecretClient SecretClient { get; }

    /// <summary>
    /// Gets the Azure Key Vault certificate client.
    /// </summary>
    /// <remarks>
    /// May be null if certificate operations are not enabled.
    /// </remarks>
    public CertificateClient? CertificateClient { get; }

    /// <inheritdoc />
    public string ServiceId { get; }
}
