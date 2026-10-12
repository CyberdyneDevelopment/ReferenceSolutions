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

namespace FdwReferenceSecretManagers.Sqlite.Configuration;

/// <summary>
/// Configuration for the SQLite secret management service.
/// Stores and retrieves secrets from a SQLite database file.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "SecretManager", ServiceType = "Sqlite")]
public sealed partial class SqliteSecretManagerConfiguration : ISecretManagerImplementationConfiguration
{
    private ILogger _logger = NullLogger<SqliteSecretManagerConfiguration>.Instance;

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
    /// Initializes a new instance of the <see cref="SqliteSecretManagerConfiguration"/> class.
    /// </summary>
    public SqliteSecretManagerConfiguration()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteSecretManagerConfiguration"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for validation and diagnostic messages.</param>
    public SqliteSecretManagerConfiguration(ILogger<SqliteSecretManagerConfiguration>? logger = null)
    {
        _logger = logger ?? NullLogger<SqliteSecretManagerConfiguration>.Instance;
    }

    #region SQLite File Properties

    /// <summary>
    /// Gets or sets the path to the SQLite database file.
    /// </summary>
    /// <remarks>Accepts any path accepted by Microsoft.Data.Sqlite (e.g. absolute path, relative path, or ":memory:").</remarks>
    public string DataSource { get; set; } = string.Empty;

    #endregion

    #region Secret Table Properties

    /// <summary>
    /// Gets or sets the name of the secrets table within the SQLite file.
    /// </summary>
    /// <value>Default: "Secret"</value>
    public string TableName { get; set; } = "Secret";

    /// <summary>
    /// Gets or sets the command timeout in seconds for SQLite operations.
    /// </summary>
    /// <value>Default: 30 seconds</value>
    public int CommandTimeoutSeconds { get; set; } = 30;

    #endregion

    /// <summary>
    /// Gets the configuration name.
    /// </summary>
    public static string ConfigurationName => "Sqlite";

    /// <summary>
    /// Gets a value indicating whether this configuration is valid.
    /// </summary>
#pragma warning disable CA1822
    public bool IsValid => !string.IsNullOrWhiteSpace(DataSource);
#pragma warning restore CA1822

    /// <summary>
    /// Gets additional configuration properties as key-value pairs.
    /// </summary>
    public IReadOnlyDictionary<string, object> Properties => CreatePropertiesDictionary();

    private Dictionary<string, object> CreatePropertiesDictionary()
    {
        return new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [nameof(DataSource)] = DataSource,
            [nameof(TableName)] = TableName,
            [nameof(CommandTimeoutSeconds)] = CommandTimeoutSeconds
        };
    }

    /// <inheritdoc/>
    public IGenericResult<ValidationResult> Validate()
    {
        var validator = new SqliteSecretManagerConfigurationValidator();
        var validationResult = validator.Validate(this);

        if (validationResult.IsValid)
        {
            return GenericResult<ValidationResult>.Success(validationResult);
        }

        var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
        return GenericResult<ValidationResult>.Failure(SecretManagerLogger.ValidationFailed(_logger, errors));
    }
}
