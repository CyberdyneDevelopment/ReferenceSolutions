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

namespace FdwReferenceSecretManagers.EnvironmentVariable.Configuration;

/// <summary>
/// Configuration for Environment Variable secret management service.
/// Inherits from SecretManagerConfiguration for common secret manager properties.
/// </summary>
/// <remarks>
/// <para>
/// This configuration inherits common properties (Id, Name, Description)
/// from <c>SecretManagerConfiguration</c> and adds EnvironmentVariable-specific
/// settings like prefix filtering, case sensitivity, and key separators for nested structures.
/// </para>
/// <para>
/// When the [ManagedConfiguration] source generator is enabled, it detects this inheritance and creates:
/// <list type="bullet">
/// <item><description>Parent table: sec.SecretManager with common columns</description></item>
/// <item><description>Child table: sec.EnvironmentVariableSecretManager with FK to parent</description></item>
/// </list>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "SecretManager",
    ServiceType = "EnvironmentVariable")]
public sealed partial class EnvironmentVariableConfiguration : ISecretManagerImplementationConfiguration
{
    private ILogger _logger = NullLogger<EnvironmentVariableConfiguration>.Instance;

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
    /// Initializes a new instance of the <see cref="EnvironmentVariableConfiguration"/> class.
    /// </summary>
    public EnvironmentVariableConfiguration()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentVariableConfiguration"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for validation and diagnostic messages.</param>
    public EnvironmentVariableConfiguration(ILogger<EnvironmentVariableConfiguration>? logger = null)
    {
        _logger = logger ?? NullLogger<EnvironmentVariableConfiguration>.Instance;
    }

    #region EnvironmentVariable Specific Properties

    /// <summary>
    /// Gets or sets a value indicating whether this configuration is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the prefix to filter environment variables.
    /// </summary>
    /// <value>The prefix string (e.g., "FDW_SECRET_", "APP_"). Only variables starting with this prefix will be available.</value>
    /// <example>FDW_SECRET_</example>
    /// <remarks>
    /// This property is required. The prefix ensures only intended environment variables
    /// are accessible as secrets, preventing accidental exposure of unrelated variables.
    /// The prefix is stripped from the key when retrieving secrets.
    /// For example, with prefix "FDW_SECRET_", environment variable "FDW_SECRET_DATABASE_PASSWORD"
    /// is accessible as "DATABASE_PASSWORD".
    /// </remarks>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether key lookups are case-sensitive.
    /// </summary>
    /// <value><c>true</c> for case-sensitive lookups; <c>false</c> for case-insensitive lookups.</value>
    /// <remarks>
    /// Environment variables on Windows are case-insensitive by default,
    /// while on Unix/Linux they are case-sensitive. Set this based on your
    /// deployment environment and naming conventions.
    /// </remarks>
    public bool CaseSensitive { get; set; }

    /// <summary>
    /// Gets or sets the separator used for nested/hierarchical key names.
    /// </summary>
    /// <value>The separator string for nested keys.</value>
    /// <remarks>
    /// Environment variables cannot contain colons or dots in most systems,
    /// so double underscore "__" is commonly used to represent hierarchy.
    /// For example, "DATABASE__CONNECTION__STRING" maps to "Database:Connection:String".
    /// </remarks>
    public string Separator { get; set; } = "__";

    /// <summary>
    /// Gets or sets a value indicating whether to strip the prefix from returned keys.
    /// </summary>
    /// <value><c>true</c> to strip the prefix; <c>false</c> to keep it.</value>
    /// <remarks>
    /// When true, environment variable "APP_DATABASE_PASSWORD" is returned
    /// as "DATABASE_PASSWORD". When false, the full name is returned.
    /// </remarks>
    public bool StripPrefix { get; set; } = true;

    /// <summary>
    /// Gets or sets the target for environment variable retrieval.
    /// </summary>
    /// <value>The environment variable target name (Process, User, or Machine).</value>
    /// <remarks>
    /// Process: Variables for the current process (default).
    /// User: Variables for the current user account.
    /// Machine: System-wide variables (requires elevated permissions to set).
    /// Stored as string to match NVARCHAR(50) column; resolved to <see cref="EnvironmentVariableTarget"/> via <see cref="TargetEnum"/>.
    /// </remarks>
    public string Target { get; set; } = nameof(EnvironmentVariableTarget.Process);

    /// <summary>
    /// Gets <see cref="Target"/> parsed to the typed enum, defaulting to <see cref="EnvironmentVariableTarget.Process"/>.
    /// </summary>
    public EnvironmentVariableTarget TargetEnum =>
        Enum.TryParse<EnvironmentVariableTarget>(Target, ignoreCase: true, out var t) ? t : EnvironmentVariableTarget.Process;

    #endregion

    /// <summary>
    /// Gets the configuration name.
    /// </summary>
    /// <value>A descriptive name for this configuration.</value>
    public static string ConfigurationName => nameof(EnvironmentVariable);

    /// <summary>
    /// Gets a value indicating whether this configuration is valid.
    /// </summary>
    /// <value><c>true</c> if the configuration is valid; otherwise, <c>false</c>.</value>
    /// <remarks>
    /// EnvironmentVariable configuration is always valid as no required parameters
    /// are needed - it can work with default settings.
    /// </remarks>
#pragma warning disable CA1822 // Mark members as static - instance property for API consistency
    public bool IsValid => true;
#pragma warning restore CA1822

    /// <summary>
    /// Gets additional configuration properties as key-value pairs.
    /// </summary>
    /// <value>A dictionary of additional configuration properties.</value>
    public IReadOnlyDictionary<string, object> Properties => CreatePropertiesDictionary();

    private Dictionary<string, object> CreatePropertiesDictionary()
    {
        var properties = new Dictionary<string, object>(StringComparer.Ordinal);

        if (!string.IsNullOrWhiteSpace(Prefix))
            properties[nameof(Prefix)] = Prefix;

        properties[nameof(CaseSensitive)] = CaseSensitive;
        properties[nameof(Separator)] = Separator;
        properties[nameof(StripPrefix)] = StripPrefix;
        properties[nameof(Target)] = Target.ToString();

        return properties;
    }

    /// <inheritdoc/>
    public IGenericResult<ValidationResult> Validate()
    {
        var validator = new EnvironmentVariableConfigurationValidator();
        var validationResult = validator.Validate(this);

        if (validationResult.IsValid)
        {
            return GenericResult<ValidationResult>.Success(validationResult);
        }

        var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
        return GenericResult<ValidationResult>.Failure(SecretManagerLogger.ValidationFailed(_logger, errors));
    }
}
