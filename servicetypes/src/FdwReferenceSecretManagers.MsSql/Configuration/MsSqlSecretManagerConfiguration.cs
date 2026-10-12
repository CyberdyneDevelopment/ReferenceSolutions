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

namespace FdwReferenceSecretManagers.MsSql.Configuration;

/// <summary>
/// Configuration for MsSql secret management service.
/// Inherits from SecretManagerConfiguration for common secret manager properties.
/// </summary>
/// <remarks>
/// <para>
/// This configuration inherits common properties (Id, Name, Description)
/// from <c>SecretManagerConfiguration</c> and adds MsSql-specific
/// settings for connecting to SQL Server and accessing the secrets table.
/// </para>
/// <para>
/// When the [ManagedConfiguration] source generator is enabled, it detects this inheritance and creates:
/// <list type="bullet">
/// <item><description>Parent table: sec.SecretManager with common columns</description></item>
/// <item><description>Child table: sec.MsSqlSecretManager with FK to parent</description></item>
/// </list>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "SecretManager",
    ServiceType = "MsSql")]
public sealed partial class MsSqlSecretManagerConfiguration : ISecretManagerImplementationConfiguration
{
    private ILogger _logger = NullLogger<MsSqlSecretManagerConfiguration>.Instance;

    // ========================================
    // ISecretManagerImplementationConfiguration — typed body identity
    // ========================================

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
    /// Initializes a new instance of the <see cref="MsSqlSecretManagerConfiguration"/> class.
    /// </summary>
    public MsSqlSecretManagerConfiguration()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlSecretManagerConfiguration"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for validation and diagnostic messages.</param>
    public MsSqlSecretManagerConfiguration(ILogger<MsSqlSecretManagerConfiguration>? logger = null)
    {
        _logger = logger ?? NullLogger<MsSqlSecretManagerConfiguration>.Instance;
    }

    #region Connection Properties

    /// <summary>
    /// Gets or sets the SQL Server hostname or IP address.
    /// </summary>
    public string Server { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the database name.
    /// </summary>
    public string Database { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SQL Server port.
    /// </summary>
    /// <value>Default: 1433</value>
    public int Port { get; set; } = 1433;

    /// <summary>
    /// Gets or sets the authentication type (e.g., "SqlAuth", "WindowsAuth").
    /// </summary>
    public string? AuthenticationType { get; set; }

    /// <summary>
    /// Gets or sets the SQL login username for SqlAuth.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the secret key name for password resolution.
    /// </summary>
    public string? SecretKeyName { get; set; }

    /// <summary>
    /// Gets or sets the secret manager name for password resolution.
    /// </summary>
    public string? SecretManagerName { get; set; }

    /// <summary>
    /// Gets or sets whether to trust the server certificate.
    /// </summary>
    public bool TrustServerCertificate { get; set; }

    /// <summary>
    /// Gets or sets whether to encrypt the connection.
    /// </summary>
    /// <value>Default: true</value>
    public bool Encrypt { get; set; } = true;

    #endregion

    #region Secret Table Properties

    /// <summary>
    /// Gets or sets the SQL schema containing the secrets table.
    /// </summary>
    /// <value>Default: "secrets"</value>
    public string Schema { get; set; } = "secrets";

    /// <summary>
    /// Gets or sets the name of the secrets table.
    /// </summary>
    /// <value>Default: "Secret"</value>
    public string TableName { get; set; } = "Secret";

    /// <summary>
    /// Gets or sets the command timeout in seconds for SQL operations.
    /// </summary>
    /// <value>Default: 30 seconds</value>
    public int CommandTimeoutSeconds { get; set; } = 30;

    #endregion

    /// <summary>
    /// Gets the configuration name.
    /// </summary>
    public static string ConfigurationName => "MsSql";

    /// <summary>
    /// Gets a value indicating whether this configuration is valid.
    /// </summary>
#pragma warning disable CA1822 // Mark members as static - instance property for API consistency
    public bool IsValid => !string.IsNullOrWhiteSpace(Server) && !string.IsNullOrWhiteSpace(Database);
#pragma warning restore CA1822

    /// <summary>
    /// Gets additional configuration properties as key-value pairs.
    /// </summary>
    public IReadOnlyDictionary<string, object> Properties => CreatePropertiesDictionary();

    private Dictionary<string, object> CreatePropertiesDictionary()
    {
        var properties = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [nameof(Server)] = Server,
            [nameof(Database)] = Database,
            [nameof(Schema)] = Schema,
            [nameof(TableName)] = TableName,
            [nameof(CommandTimeoutSeconds)] = CommandTimeoutSeconds
        };

        return properties;
    }

    /// <inheritdoc/>
    public IGenericResult<ValidationResult> Validate()
    {
        var validator = new MsSqlSecretManagerConfigurationValidator();
        var validationResult = validator.Validate(this);

        if (validationResult.IsValid)
        {
            return GenericResult<ValidationResult>.Success(validationResult);
        }

        var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
        return GenericResult<ValidationResult>.Failure(SecretManagerLogger.ValidationFailed(_logger, errors));
    }
}
