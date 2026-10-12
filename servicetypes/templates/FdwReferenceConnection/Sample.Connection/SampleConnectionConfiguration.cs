using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Services.Connections.Abstractions;

namespace Sample.Connection;

/// <summary>What a SAMPLE_CONNECTION connection is configured by.</summary>
/// <remarks>
/// This is the implementation configuration, not the domain record. The domain record (a
/// <c>DomainConfiguration</c> row in <c>conn.Connection</c>) carries the name and the
/// <c>Implementation</c> that selects this kind; this type carries what every connection
/// implementation has — description, environment, health checking, discovery — plus what THIS kind
/// needs to open a connection, and points back at its domain record through <see cref="ConnectionId"/>.
/// That split is why adding a connection kind adds a type here rather than columns to everyone
/// else's record.
///
/// Name, Domain and Implementation belong to the domain record and are never persisted on this
/// one: the domain provider stamps them from the domain row on every read, so anything holding
/// this configuration knows its name and kind without reading the domain row again. What a caller
/// actually configures stays unset: a missing server must fail where it is read, not become
/// something plausible here.
/// </remarks>
[ExcludeFromCodeCoverage]
public class SampleConnectionConfiguration : IConnectionImplementationConfiguration
{
    /// <summary>Gets or sets this configuration row's own identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the identifier of the domain record that owns this configuration.</summary>
    public Guid ConnectionId { get; set; }

    /// <summary>Gets or sets the name this configuration is resolved by.</summary>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the domain this implementation belongs to.</summary>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;

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

    /// <summary>Gets or sets the host this connection reaches.</summary>
    public string? Server { get; set; }

    /// <summary>Gets or sets how this connection authenticates.</summary>
    public string? AuthenticationType { get; set; }
}
