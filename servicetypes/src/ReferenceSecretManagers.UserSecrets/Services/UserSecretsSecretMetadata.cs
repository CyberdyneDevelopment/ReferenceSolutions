using System;
using System.Collections.Generic;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.UserSecrets.Commands;
using Fdw.Services.SecretManagers.UserSecrets.Configuration;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.UserSecrets.Services;

/// <summary>
/// User Secrets implementation of secret metadata.
/// </summary>
public sealed class UserSecretsSecretMetadata : ISecretMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserSecretsSecretMetadata"/> class.
    /// </summary>
    /// <param name="key">The secret key.</param>
    /// <param name="createdAt">The creation timestamp (file modification time).</param>
    /// <param name="modifiedAt">The modification timestamp (file modification time).</param>
    /// <param name="secretsFilePath">The path to the secrets file.</param>
    public UserSecretsSecretMetadata(
        string key,
        DateTimeOffset createdAt,
        DateTimeOffset modifiedAt,
        string? secretsFilePath = null)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        CreatedAt = createdAt;
        ModifiedAt = modifiedAt;
        SecretsFilePath = secretsFilePath;

        // Get properties dictionary
        var properties = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["Provider"] = "UserSecrets",
            ["ReadOnly"] = true
        };

        if (!string.IsNullOrWhiteSpace(secretsFilePath))
            properties["FilePath"] = secretsFilePath;

        properties[nameof(CreatedAt)] = createdAt;
        properties[nameof(ModifiedAt)] = modifiedAt;

        Properties = properties;
    }

    /// <inheritdoc/>
    public string Key { get; }

    /// <inheritdoc/>
    public string? Container => SecretsFilePath;

    /// <inheritdoc/>
    public string? Version => null; // User Secrets don't support versioning

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; }

    /// <inheritdoc/>
    public DateTimeOffset ModifiedAt { get; }

    /// <inheritdoc/>
    public DateTimeOffset? ExpiresAt => null; // User Secrets don't support expiration

    /// <inheritdoc/>
    public string? CreatedBy => null; // Not tracked

    /// <inheritdoc/>
    public string? ModifiedBy => null; // Not tracked

    /// <inheritdoc/>
    public bool IsExpired => false; // User Secrets don't support expiration

    /// <inheritdoc/>
    public bool IsEnabled => true; // Always enabled

    /// <inheritdoc/>
    public bool IsBinary => false; // User Secrets are always text

    /// <inheritdoc/>
    public long SizeInBytes => 0; // Not tracked without reading the value

    /// <inheritdoc/>
    public IReadOnlyCollection<string> Tags => Array.Empty<string>();

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object> Properties { get; }

    /// <inheritdoc/>
    public IReadOnlyCollection<string> AvailableVersions => Array.Empty<string>();

    /// <inheritdoc/>
    public string? AccessPolicy => null; // Not applicable

    /// <inheritdoc/>
    public string? EncryptionMethod => null; // No encryption at rest

    /// <summary>
    /// Gets the path to the secrets file.
    /// </summary>
    public string? SecretsFilePath { get; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"UserSecrets Secret: {Key}";
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is UserSecretsSecretMetadata other &&
               string.Equals(Key, other.Key, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return Key.GetHashCode(StringComparison.Ordinal);
    }
}
