using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Connections.Abstractions;

namespace FdwReferenceConnections.FileSystem;

/// <summary>
/// Configuration for FileSystem connections.
/// Standalone typed body POCO — no longer inherits from <c>ConnectionConfiguration</c>.
/// Persisted to <c>conn.FileSystemConnectionConfiguration</c> under its <c>conn.Connection</c> row, through the
/// Foreign key that container declares to the connection's container.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Connection", ServiceType = "FileSystem")]
public partial class FileSystemConnectionConfiguration : IConnectionImplementationConfiguration
{
    /// <inheritdoc/>
    public string? Description { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset CreateDate { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset ModifyDate { get; set; }

    /// <inheritdoc/>
    public string? Environment { get; set; }

    /// <inheritdoc/>
    public bool HealthCheckEnabled { get; set; }

    /// <inheritdoc/>
    public bool HealthCheckOnStartup { get; set; }

    /// <inheritdoc/>
    public int? HealthCheckIntervalSeconds { get; set; }

    /// <inheritdoc/>
    [ValuesFrom(typeof(SchemaDiscoveryTypes))]
    public string SchemaDiscovery { get; set; } = string.Empty;

    // ========================================
    // IGenericConfiguration — typed body identity
    // ========================================

    /// <summary>
    /// Gets or sets the unique identifier for this typed body row (conn.FileSystemConnection.Id).
    /// Minted by <c>ImplementationConfigurationProviderBase{TConfig,TCommand}</c>
    /// via <see cref="Guid.CreateVersion7()"/> when <see cref="Guid.Empty"/>.
    /// </summary>
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


    // ========================================
    // FileSystem-specific properties
    // ========================================

    /// <summary>
    /// Gets or sets the root directory path for this file system connection.
    /// </summary>
    public string Root { get; set; } = string.Empty;
}
