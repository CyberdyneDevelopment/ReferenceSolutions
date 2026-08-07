using System;
using System.Collections.Generic;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.MsSql.Commands;
using Fdw.Services.SecretManagers.MsSql.Configuration;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.MsSql.Services;

/// <summary>
/// Represents metadata for a secret stored in SQL Server.
/// </summary>
public sealed class MsSqlSecretMetadata : ISecretMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlSecretMetadata"/> class.
    /// </summary>
    /// <param name="key">The secret key.</param>
    /// <param name="schema">The SQL schema name.</param>
    /// <param name="tableName">The SQL table name.</param>
    /// <param name="version">The secret version.</param>
    /// <param name="secretType">The type of secret (e.g., Password, ApiKey).</param>
    /// <param name="description">Optional description.</param>
    /// <param name="createdAt">When the secret was created.</param>
    /// <param name="modifiedAt">When the secret was last modified.</param>
    /// <param name="expiresAt">When the secret expires, if applicable.</param>
    public MsSqlSecretMetadata(
        string key,
        string schema,
        string tableName,
        int version,
        string secretType,
        string? description,
        DateTimeOffset createdAt,
        DateTimeOffset modifiedAt,
        DateTimeOffset? expiresAt)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        Version = version.ToString(System.Globalization.CultureInfo.InvariantCulture);
        CreatedAt = createdAt;
        ModifiedAt = modifiedAt;
        ExpiresAt = expiresAt;

        Properties = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["Schema"] = schema,
            ["TableName"] = tableName,
            ["SecretType"] = secretType
        };

        if (description is not null)
        {
            ((Dictionary<string, object>)Properties)["Description"] = description;
        }
    }

    /// <inheritdoc/>
    public string Key { get; }

    /// <inheritdoc/>
    public string? Container => "MsSql";

    /// <inheritdoc/>
    public string? Version { get; }

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; }

    /// <inheritdoc/>
    public DateTimeOffset ModifiedAt { get; }

    /// <inheritdoc/>
    public DateTimeOffset? ExpiresAt { get; }

    /// <inheritdoc/>
    public string? CreatedBy => null;

    /// <inheritdoc/>
    public string? ModifiedBy => null;

    /// <inheritdoc/>
    public bool IsExpired => ExpiresAt.HasValue && DateTimeOffset.UtcNow > ExpiresAt.Value;

    /// <inheritdoc/>
    public bool IsEnabled => true;

    /// <inheritdoc/>
    public bool IsBinary => false;

    /// <inheritdoc/>
    public long SizeInBytes => 0; // Not tracked without reading the value

    /// <inheritdoc/>
    public IReadOnlyCollection<string> Tags => Array.Empty<string>();

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object> Properties { get; }

    /// <inheritdoc/>
    public IReadOnlyCollection<string> AvailableVersions => Array.Empty<string>();

    /// <inheritdoc/>
    public string? AccessPolicy => null;

    /// <inheritdoc/>
    public string? EncryptionMethod => null;
}
