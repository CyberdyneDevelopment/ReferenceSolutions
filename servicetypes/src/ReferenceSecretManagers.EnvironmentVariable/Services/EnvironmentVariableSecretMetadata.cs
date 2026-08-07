using System;
using System.Collections.Generic;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.EnvironmentVariable.Commands;
using Fdw.Services.SecretManagers.EnvironmentVariable.Configuration;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.EnvironmentVariable.Services;

/// <summary>
/// Represents metadata for an environment variable secret.
/// </summary>
/// <remarks>
/// Environment variables have limited metadata compared to full secret management systems.
/// Many properties return default or null values as environment variables don't track
/// creation time, modification time, versions, etc.
/// </remarks>
public sealed class EnvironmentVariableSecretMetadata : ISecretMetadata
{
    private readonly string _environmentVariableName;
    private readonly EnvironmentVariableTarget _target;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentVariableSecretMetadata"/> class.
    /// </summary>
    /// <param name="key">The secret key (possibly with prefix stripped).</param>
    /// <param name="environmentVariableName">The actual environment variable name.</param>
    /// <param name="target">The environment variable target.</param>
    public EnvironmentVariableSecretMetadata(
        string key,
        string environmentVariableName,
        EnvironmentVariableTarget target)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        _environmentVariableName = environmentVariableName ?? throw new ArgumentNullException(nameof(environmentVariableName));
        _target = target;

        Properties = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["EnvironmentVariableName"] = _environmentVariableName,
            ["Target"] = _target.ToString()
        };
    }

    /// <inheritdoc/>
    public string Key { get; }

    /// <inheritdoc/>
    public string? Container => _target.ToString();

    /// <inheritdoc/>
    public string? Version => null; // Environment variables don't have versions

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt => DateTimeOffset.MinValue; // Unknown

    /// <inheritdoc/>
    public DateTimeOffset ModifiedAt => DateTimeOffset.MinValue; // Unknown

    /// <inheritdoc/>
    public DateTimeOffset? ExpiresAt => null; // Environment variables don't expire

    /// <inheritdoc/>
    public string? CreatedBy => null; // Unknown

    /// <inheritdoc/>
    public string? ModifiedBy => null; // Unknown

    /// <inheritdoc/>
    public bool IsExpired => false; // Environment variables don't expire

    /// <inheritdoc/>
    public bool IsEnabled => true; // Always enabled if present

    /// <inheritdoc/>
    public bool IsBinary => false; // Environment variables are always strings

    /// <inheritdoc/>
    public long SizeInBytes
    {
        get
        {
            var value = Environment.GetEnvironmentVariable(_environmentVariableName, _target);
            return value?.Length * sizeof(char) ?? 0;
        }
    }

    /// <inheritdoc/>
    public IReadOnlyCollection<string> Tags => Array.Empty<string>();

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object> Properties { get; }

    /// <inheritdoc/>
    public IReadOnlyCollection<string> AvailableVersions => Array.Empty<string>();

    /// <inheritdoc/>
    public string? AccessPolicy => null;

    /// <inheritdoc/>
    public string? EncryptionMethod => null; // Environment variables are not encrypted at rest
}
