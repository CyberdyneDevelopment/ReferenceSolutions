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

namespace FdwReferenceSecretManagers.UserSecrets.Configuration;

/// <summary>
/// Configuration for .NET User Secrets secret management service.
/// Inherits from SecretManagerConfiguration for common secret manager properties.
/// </summary>
/// <remarks>
/// <para>
/// This configuration inherits common properties (Id, Name, Description)
/// from <c>SecretManagerConfiguration</c> and adds UserSecrets-specific
/// settings for accessing .NET User Secrets stored in the local user profile.
/// </para>
/// <para>
/// When the [ManagedConfiguration] source generator is enabled, it detects this inheritance and creates:
/// <list type="bullet">
/// <item><description>Parent table: sec.SecretManager with common columns</description></item>
/// <item><description>Child table: sec.UserSecretsSecretManager with FK to parent</description></item>
/// </list>
/// </para>
/// <para>
/// User Secrets are typically used for development-time secrets and are stored at:
/// </para>
/// <list type="bullet">
/// <item><description>Windows: %APPDATA%\Microsoft\UserSecrets\{userSecretsId}\secrets.json</description></item>
/// <item><description>Linux/macOS: ~/.microsoft/usersecrets/{userSecretsId}/secrets.json</description></item>
/// </list>
/// <para>
/// This is a read-only secret manager implementation. Only GetSecret and ListSecrets operations are supported.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "SecretManager",
    ServiceType = "UserSecrets")]
public sealed partial class UserSecretsConfiguration : ISecretManagerImplementationConfiguration
{
    private ILogger _logger = NullLogger<UserSecretsConfiguration>.Instance;

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
    /// Initializes a new instance of the <see cref="UserSecretsConfiguration"/> class.
    /// </summary>
    public UserSecretsConfiguration()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserSecretsConfiguration"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for validation and diagnostic messages.</param>
    public UserSecretsConfiguration(ILogger<UserSecretsConfiguration>? logger = null)
    {
        _logger = logger ?? NullLogger<UserSecretsConfiguration>.Instance;
    }

    #region UserSecrets Specific Properties

    /// <summary>
    /// Gets or sets a value indicating whether this configuration is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the User Secrets ID used to locate the secrets.json file.
    /// </summary>
    /// <value>The unique identifier for the user secrets collection.</value>
    /// <remarks>
    /// This is typically a GUID that matches the UserSecretsId in a project's .csproj file.
    /// For example: &lt;UserSecretsId&gt;79a3edd0-2092-40a2-a04d-dcb46d5ca9ed&lt;/UserSecretsId&gt;
    /// </remarks>
    public string? UserSecretsId { get; set; }

    /// <summary>
    /// Gets or sets an optional override for the secrets file path.
    /// </summary>
    /// <value>The full path to the secrets.json file, or null to use the default location.</value>
    /// <remarks>
    /// <para>
    /// When specified, this path takes precedence over the <see cref="UserSecretsId"/> property.
    /// The path should point directly to the secrets.json file.
    /// </para>
    /// <para>
    /// Default locations if not specified:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Windows: %APPDATA%\Microsoft\UserSecrets\{UserSecretsId}\secrets.json</description></item>
    /// <item><description>Linux/macOS: ~/.microsoft/usersecrets/{UserSecretsId}/secrets.json</description></item>
    /// </list>
    /// </remarks>
    public string? SecretsFilePath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to reload secrets when the file changes.
    /// </summary>
    /// <value><c>true</c> to watch for file changes and reload; otherwise, <c>false</c>.</value>
    /// <remarks>
    /// Defaults to true for development scenarios where secrets may be updated frequently.
    /// </remarks>
    public bool ReloadOnChange { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to fail silently if the secrets file doesn't exist.
    /// </summary>
    /// <value><c>true</c> to return empty results when file is missing; <c>false</c> to return an error.</value>
    /// <remarks>
    /// Defaults to true for graceful handling in non-development environments where
    /// user secrets may not be configured.
    /// </remarks>
    public bool Optional { get; set; } = true;

    #endregion

    /// <summary>
    /// Gets the configuration name.
    /// </summary>
    /// <value>A descriptive name for this configuration.</value>
    public static string ConfigurationName => nameof(UserSecrets);

    /// <summary>
    /// Gets a value indicating whether this configuration is valid.
    /// </summary>
    /// <value><c>true</c> if the configuration is valid; otherwise, <c>false</c>.</value>
    /// <remarks>
    /// A valid configuration must have either a UserSecretsId or a SecretsFilePath specified.
    /// </remarks>
    public bool IsValid => !string.IsNullOrWhiteSpace(UserSecretsId) ||
                           !string.IsNullOrWhiteSpace(SecretsFilePath);

    /// <summary>
    /// Gets additional configuration properties as key-value pairs.
    /// </summary>
    /// <value>A dictionary of additional configuration properties.</value>
    public IReadOnlyDictionary<string, object> Properties => CreatePropertiesDictionary();

    private Dictionary<string, object> CreatePropertiesDictionary()
    {
        var properties = new Dictionary<string, object>(StringComparer.Ordinal);

        if (!string.IsNullOrWhiteSpace(UserSecretsId))
            properties[nameof(UserSecretsId)] = UserSecretsId;

        if (!string.IsNullOrWhiteSpace(SecretsFilePath))
            properties[nameof(SecretsFilePath)] = SecretsFilePath;

        properties[nameof(ReloadOnChange)] = ReloadOnChange;
        properties[nameof(Optional)] = Optional;

        return properties;
    }

    /// <inheritdoc/>
    public IGenericResult<ValidationResult> Validate()
    {
        var validator = new UserSecretsConfigurationValidator();
        var validationResult = validator.Validate(this);

        if (validationResult.IsValid)
        {
            return GenericResult<ValidationResult>.Success(validationResult);
        }

        var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
        return GenericResult<ValidationResult>.Failure(SecretManagerLogger.ValidationFailed(_logger, errors));
    }
}
