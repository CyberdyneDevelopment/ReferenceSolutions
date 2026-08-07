using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Security.KeyVault.Secrets;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManager;
using ReferenceSecretManagers.AzureKeyVault.Commands;
using Fdw.Services.SecretManagers.AzureKeyVault.Configuration;
using Fdw.Services.SecretManagers.AzureKeyVault.CredentialTypes;
using Fdw.Services.SecretManagers.AzureKeyVault.Commands;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.AzureKeyVault.Services;

/// <summary>
/// Azure Key Vault implementation of secret metadata.
/// </summary>
public sealed class AzureKeyVaultSecretMetadata : ISecretMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzureKeyVaultSecretMetadata"/> class.
    /// </summary>
    /// <param name="name">The secret name.</param>
    /// <param name="version">The secret version.</param>
    /// <param name="createdOn">The creation timestamp.</param>
    /// <param name="updatedOn">The last update timestamp.</param>
    /// <param name="expiresOn">The expiration timestamp.</param>
    /// <param name="enabled">Whether the secret is enabled.</param>
    /// <param name="tags">The secret tags.</param>
    /// <param name="vaultName">The name of the Azure Key Vault.</param>
    public AzureKeyVaultSecretMetadata(
        string name,
        string? version,
        DateTimeOffset? createdOn,
        DateTimeOffset? updatedOn,
        DateTimeOffset? expiresOn,
        bool enabled,
        IReadOnlyDictionary<string, string>? tags,
        string? vaultName = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Version = version;
        CreatedOn = createdOn;
        UpdatedOn = updatedOn;
        ExpiresOn = expiresOn;
        IsEnabled = enabled;
        VaultName = vaultName;
        Tags = tags ?? new Dictionary<string, string>(StringComparer.Ordinal);

        // Set other properties based on Azure Key Vault behavior
        IsDeleted = false; // Would need to check DeletedOn in real implementation
        RecoveryLevel = "Recoverable+Purgeable"; // Default for most Azure Key Vault configurations

        // Get properties dictionary
        var properties = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["Provider"] = nameof(AzureKeyVault),
            ["VaultType"] = "Standard" // Could be Premium in some cases
        };

        if (!string.IsNullOrWhiteSpace(version))
            properties[nameof(Version)] = version;

        if (createdOn.HasValue)
            properties[nameof(CreatedOn)] = createdOn.Value;

        if (updatedOn.HasValue)
            properties[nameof(UpdatedOn)] = updatedOn.Value;

        if (expiresOn.HasValue)
            properties[nameof(ExpiresOn)] = expiresOn.Value;

        properties["Enabled"] = enabled;
        properties[nameof(RecoveryLevel)] = RecoveryLevel;

        Properties = properties;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureKeyVaultSecretMetadata"/> class from Azure SDK SecretProperties.
    /// </summary>
    /// <param name="properties">The Azure SDK secret properties.</param>
    public AzureKeyVaultSecretMetadata(SecretProperties properties)
        : this(
            name: properties.Name,
            version: properties.Version,
            createdOn: properties.CreatedOn,
            updatedOn: properties.UpdatedOn,
            expiresOn: properties.ExpiresOn,
            enabled: properties.Enabled ?? true,
            tags: properties.Tags?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.Ordinal),
            vaultName: properties.VaultUri?.Host)
    {
    }

    /// <inheritdoc/>
    public string Key => Name;

    /// <inheritdoc/>
    public string? Container => VaultName;

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public string? Version { get; }

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt => CreatedOn ?? DateTimeOffset.MinValue;

    /// <inheritdoc/>
    public DateTimeOffset ModifiedAt => UpdatedOn ?? DateTimeOffset.MinValue;

    /// <inheritdoc/>
    public DateTimeOffset? ExpiresAt => ExpiresOn;

    /// <inheritdoc/>
    public string? CreatedBy => null; // Azure Key Vault doesn't provide this directly

    /// <inheritdoc/>
    public string? ModifiedBy => null; // Azure Key Vault doesn't provide this directly

    /// <inheritdoc/>
    public bool IsExpired => ExpiresOn.HasValue && ExpiresOn.Value <= DateTimeOffset.UtcNow;

    /// <inheritdoc/>
    public bool IsEnabled { get; }

    /// <inheritdoc/>
    public bool IsBinary => false; // Azure Key Vault secrets are text-based

    /// <inheritdoc/>
    public long SizeInBytes { get; private set; } // Would need actual size calculation

    /// <inheritdoc/>
    IReadOnlyCollection<string> ISecretMetadata.Tags => Tags.Keys.ToList().AsReadOnly();

    /// <inheritdoc/>
    public IReadOnlyCollection<string> AvailableVersions { get; private set; } = [];

    /// <inheritdoc/>
    public string? AccessPolicy => null; // Would need specific implementation

    /// <inheritdoc/>
    public string? EncryptionMethod => "AES-256"; // Azure Key Vault default

    // Azure Key Vault specific properties
    /// <inheritdoc/>
    public DateTimeOffset? CreatedOn { get; }

    /// <inheritdoc/>
    public DateTimeOffset? UpdatedOn { get; }

    /// <inheritdoc/>
    public DateTimeOffset? ExpiresOn { get; }

    /// <inheritdoc/>
    public bool IsDeleted { get; }

    /// <inheritdoc/>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Interface implementation cannot be static")]
    public DateTimeOffset? DeletedOn => null; // Not provided in this simple implementation

    /// <inheritdoc/>
    public string? RecoveryLevel { get; }

    /// <inheritdoc/>
    public string? VaultName { get; private set; }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string> Tags { get; }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object> Properties { get; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"AzureKeyVault Secret: {Name} (Version: {Version ?? "latest"}, Enabled: {IsEnabled})";
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is AzureKeyVaultSecretMetadata other &&
               string.Equals(Name, other.Name, StringComparison.Ordinal) &&
               string.Equals(Version, other.Version, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(Name, Version);
    }
}