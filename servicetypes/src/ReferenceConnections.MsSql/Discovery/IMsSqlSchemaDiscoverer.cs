using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

using Fdw.Services.Connections.MsSql;
using Fdw.Services.Connections.MsSql.Discovery;
using Fdw.Services.Connections.MsSql.Logging;

using ReferenceConnections.MsSql;

using ReferenceConnections.MsSql.Discovery;

namespace ReferenceConnections.MsSql.Discovery;

/// <summary>
/// Service for discovering SQL Server database schemas to discover tables, views, and their columns.
/// </summary>
/// <remarks>
/// This discoverer works with an existing MsSqlConnection to query INFORMATION_SCHEMA
/// and system tables for schema metadata. It's designed for lazy/on-demand discovery
/// when containers are not pre-configured.
/// </remarks>
public interface IMsSqlSchemaDiscoverer
{
    /// <summary>
    /// Discovers the schema of a database using the provided connection.
    /// </summary>
    /// <param name="connection">The MsSql connection to use for discovery.</param>
    /// <param name="options">Options controlling the discovery behavior.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the discovered schema, or failure information.</returns>
    Task<IGenericResult<SchemaDiscoveryResult>> DiscoverSchema(
        MsSqlConnection connection,
        SchemaDiscoveryOptions? options,
        CancellationToken cancellationToken);

    /// <summary>
    /// Discovers the schema of a single table or view.
    /// </summary>
    /// <param name="connection">The MsSql connection to use for discovery.</param>
    /// <param name="schemaName">The database schema name (e.g., "dbo").</param>
    /// <param name="objectName">The table or view name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the discovered container, or failure information.</returns>
    Task<IGenericResult<DiscoveredContainer>> DiscoverContainer(
        MsSqlConnection connection,
        string schemaName,
        string objectName,
        CancellationToken cancellationToken);

    /// <summary>
    /// Tests whether discovery is possible with the current connection.
    /// </summary>
    /// <param name="connection">The MsSql connection to test.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating whether discovery can proceed.</returns>
    Task<IGenericResult<bool>> TestDiscoveryCapability(
        MsSqlConnection connection,
        CancellationToken cancellationToken);
}
