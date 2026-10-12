using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Results;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Abstractions.Logging;
using FdwReferenceSecretManagers.AzureKeyVault.CredentialTypes;

namespace FdwReferenceSecretManagers.AzureKeyVault.Configuration;

/// <summary>
/// Configuration for Azure Key Vault secret management service.
/// Inherits from SecretManagerConfiguration for common secret manager properties.
/// </summary>
/// <remarks>
/// <para>
/// This configuration inherits common properties (Id, Name, Description)
/// from <c>SecretManagerConfiguration</c> and adds AzureKeyVault-specific
/// settings for connecting to and authenticating with Azure Key Vault.
/// </para>
/// <para>
/// When the [ManagedConfiguration] source generator is enabled, it detects this inheritance and creates:
/// <list type="bullet">
/// <item><description>Parent table: sec.SecretManager with common columns</description></item>
/// <item><description>Child table: sec.AzureKeyVaultSecretManager with FK to parent</description></item>
/// </list>
/// </para>
/// <para>
/// Supports multiple authentication methods including managed identity, service principal,
/// and certificate-based authentication.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "SecretManager",
    ServiceType = "AzureKeyVault")]
public sealed partial class AzureKeyVaultConfiguration : ISecretManagerImplementationConfiguration
{
    private readonly ILogger<AzureKeyVaultConfiguration> _logger;

    /// <summary>Gets or sets the logical identity of this configuration record.</summary>
    // Why: NO Guid.NewGuid() default — the provider mints this before INSERT via CreateVersion7().
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name this configuration is resolved by.</summary>
    // Why it maps with no column of its own: the name is the domain's, and there is one
    // name for a configured member. The domain provider joins the domain row to this one,
    // and the join's result set is what carries it in.
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the domain this implementation belongs to.</summary>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureKeyVaultConfiguration"/> class.
    /// </summary>
    public AzureKeyVaultConfiguration()
    {
        _logger = NullLogger<AzureKeyVaultConfiguration>.Instance;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureKeyVaultConfiguration"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for validation and diagnostic messages.</param>
    public AzureKeyVaultConfiguration(ILogger<AzureKeyVaultConfiguration>? logger)
    {
        _logger = logger ?? NullLogger<AzureKeyVaultConfiguration>.Instance;
    }

    #region AzureKeyVault Specific Properties

    /// <summary>
    /// Gets or sets a value indicating whether this configuration is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the Azure Key Vault URI.
    /// </summary>
    /// <value>The full URI to the Azure Key Vault instance.</value>
    /// <example>https://myvault.vault.azure.net/</example>
    public string? VaultUri { get; set; }

    /// <summary>
    /// Gets or sets the authentication method to use.
    /// </summary>
    /// <value>The authentication method identifier.</value>
    /// <remarks>
    /// Supported values: "ManagedIdentity", "ServicePrincipal", "Certificate", "DeviceCode"
    /// </remarks>
    [ValuesFrom(typeof(AzureCredentialTypes))]
    public string? AuthenticationMethod { get; set; }

    /// <summary>
    /// Gets or sets the Azure tenant ID.
    /// </summary>
    /// <value>The Azure Active Directory tenant ID.</value>
    /// <remarks>
    /// Required for service principal and certificate authentication.
    /// </remarks>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the Azure client ID (application ID).
    /// </summary>
    /// <value>The Azure application (client) ID.</value>
    /// <remarks>
    /// Required for service principal and certificate authentication.
    /// </remarks>
    public string? ClientId { get; set; }

    /// <summary>
    /// Gets or sets the Azure client secret.
    /// </summary>
    /// <value>The Azure application client secret.</value>
    /// <remarks>
    /// Required for service principal authentication with client secret.
    /// Should be stored securely and not logged.
    /// </remarks>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Gets or sets the path to the client certificate file.
    /// </summary>
    /// <value>The file path to the X.509 certificate file (.pfx or .p12).</value>
    /// <remarks>
    /// Required for certificate-based authentication.
    /// </remarks>
    public string? CertificatePath { get; set; }

    /// <summary>
    /// Gets or sets the certificate password.
    /// </summary>
    /// <value>The password for the certificate file.</value>
    /// <remarks>
    /// Required if the certificate file is password-protected.
    /// Should be stored securely and not logged.
    /// </remarks>
    public string? CertificatePassword { get; set; }

    /// <summary>
    /// Gets or sets the connection timeout for Key Vault operations.
    /// </summary>
    /// <value>The timeout duration for individual operations.</value>
    /// <remarks>
    /// Defaults to 30 seconds if not specified.
    /// </remarks>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Gets or sets the retry policy configuration.
    /// </summary>
    /// <value>A dictionary of retry policy settings.</value>
    /// <remarks>
    /// Common settings include: MaxRetries, InitialDelay, MaxDelay, BackoffMultiplier
    /// </remarks>
    public IReadOnlyDictionary<string, object>? RetryPolicy { get; set; }

    /// <summary>
    /// Gets or sets additional headers to include in Key Vault requests.
    /// </summary>
    /// <value>A dictionary of HTTP headers to add to requests.</value>
    /// <remarks>
    /// Useful for adding custom tracking headers or compliance requirements.
    /// </remarks>
    public IReadOnlyDictionary<string, string>? AdditionalHeaders { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable distributed tracing.
    /// </summary>
    /// <value><c>true</c> to enable distributed tracing; otherwise, <c>false</c>.</value>
    /// <remarks>
    /// When enabled, Key Vault operations will be traced for observability.
    /// </remarks>
    public bool EnableTracing { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to validate the Key Vault URI on startup.
    /// </summary>
    /// <value><c>true</c> to validate the URI on startup; otherwise, <c>false</c>.</value>
    /// <remarks>
    /// When enabled, the service will attempt to connect to Key Vault during initialization
    /// to validate configuration and permissions.
    /// </remarks>
    public bool ValidateOnStartup { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of secrets to retrieve in a single list operation.
    /// </summary>
    /// <value>The maximum number of secrets per page.</value>
    /// <remarks>
    /// Defaults to 25 if not specified. Azure Key Vault supports up to 25 items per page.
    /// </remarks>
    public int? MaxSecretsPerPage { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include deleted secrets in list operations.
    /// </summary>
    /// <value><c>true</c> to include deleted secrets by default; otherwise, <c>false</c>.</value>
    /// <remarks>
    /// This setting affects the default behavior of list operations.
    /// Individual commands can override this setting.
    /// </remarks>
    public bool IncludeDeletedByDefault { get; set; }

    /// <summary>
    /// Gets or sets the resource identifier for managed identity authentication.
    /// </summary>
    /// <value>The managed identity resource identifier.</value>
    /// <remarks>
    /// Used when multiple managed identities are available.
    /// Can be a client ID, object ID, or resource ID.
    /// </remarks>
    public string? ManagedIdentityId { get; set; }

    #endregion

    /// <summary>
    /// Gets the configuration name.
    /// </summary>
    /// <value>A descriptive name for this configuration.</value>
    public static string ConfigurationName => nameof(AzureKeyVault);

    /// <summary>
    /// Gets a value indicating whether this configuration is valid.
    /// </summary>
    /// <value><c>true</c> if the configuration is valid; otherwise, <c>false</c>.</value>
    public bool IsValid => !string.IsNullOrWhiteSpace(VaultUri) &&
                           !string.IsNullOrWhiteSpace(AuthenticationMethod);

    /// <summary>
    /// Gets additional configuration properties as key-value pairs.
    /// </summary>
    /// <value>A dictionary of additional configuration properties.</value>
    public IReadOnlyDictionary<string, object> Properties => CreatePropertiesDictionary();

    private Dictionary<string, object> CreatePropertiesDictionary()
    {
        var properties = new Dictionary<string, object>(StringComparer.Ordinal);

        if (!string.IsNullOrWhiteSpace(VaultUri))
            properties[nameof(VaultUri)] = VaultUri;

        if (!string.IsNullOrWhiteSpace(AuthenticationMethod))
            properties[nameof(AuthenticationMethod)] = AuthenticationMethod;

        if (!string.IsNullOrWhiteSpace(TenantId))
            properties[nameof(TenantId)] = TenantId;

        if (!string.IsNullOrWhiteSpace(ClientId))
            properties[nameof(ClientId)] = ClientId;

        if (Timeout.HasValue)
            properties[nameof(Timeout)] = Timeout.Value;

        if (EnableTracing)
            properties[nameof(EnableTracing)] = EnableTracing;

        if (ValidateOnStartup)
            properties[nameof(ValidateOnStartup)] = ValidateOnStartup;

        if (MaxSecretsPerPage.HasValue)
            properties[nameof(MaxSecretsPerPage)] = MaxSecretsPerPage.Value;

        if (IncludeDeletedByDefault)
            properties[nameof(IncludeDeletedByDefault)] = IncludeDeletedByDefault;

        if (!string.IsNullOrWhiteSpace(ManagedIdentityId))
            properties[nameof(ManagedIdentityId)] = ManagedIdentityId;

        return properties;
    }

    /// <inheritdoc/>
    public IGenericResult<ValidationResult> Validate()
    {
        var validator = new AzureKeyVaultConfigurationValidator();
        var validationResult = validator.Validate(this);

        if (validationResult.IsValid)
        {
            return GenericResult<ValidationResult>.Success(validationResult);
        }

        var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
        return GenericResult<ValidationResult>.Failure(SecretManagerLogger.ValidationFailed(_logger, errors));
    }
}
