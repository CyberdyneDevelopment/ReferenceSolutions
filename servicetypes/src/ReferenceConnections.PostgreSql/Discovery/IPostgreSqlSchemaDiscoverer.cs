using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

using Fdw.Services.Connections.PostgreSql;
using Fdw.Services.Connections.PostgreSql.Discovery;
using Fdw.Services.Connections.PostgreSql.Logging;

using ReferenceConnections.PostgreSql;

namespace ReferenceConnections.PostgreSql.Discovery;

/// <summary>
/// Service for discovering PostgreSQL database schemas to discover tables, views, and their columns.
/// </summary>
/// <remarks>
/// This discoverer works with an existing PostgreSqlConnection to query information_schema
/// and pg_catalog for schema metadata. It's designed for lazy/on-demand discovery
/// when containers are not pre-configured.
/// </remarks>
public interface IPostgreSqlSchemaDiscoverer
{
    /// <summary>
    /// Discovers the schema of a database using the provided connection.
    /// </summary>
    /// <param name="connection">The PostgreSQL connection to use for discovery.</param>
    /// <param name="options">Options controlling the discovery behavior.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the discovered schema, or failure information.</returns>
    Task<IGenericResult<SchemaDiscoveryResult>> DiscoverSchema(
        PostgreSqlConnection connection,
        SchemaDiscoveryOptions? options,
        CancellationToken cancellationToken);

    /// <summary>
    /// Discovers the schema of a single table or view.
    /// </summary>
    /// <param name="connection">The PostgreSQL connection to use for discovery.</param>
    /// <param name="schemaName">The database schema name (e.g., "public").</param>
    /// <param name="objectName">The table or view name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the discovered container, or failure information.</returns>
    Task<IGenericResult<DiscoveredContainer>> DiscoverContainer(
        PostgreSqlConnection connection,
        string schemaName,
        string objectName,
        CancellationToken cancellationToken);

    /// <summary>
    /// Tests whether discovery is possible with the current connection.
    /// </summary>
    /// <param name="connection">The PostgreSQL connection to test.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating whether discovery can proceed.</returns>
    Task<IGenericResult<bool>> TestDiscoveryCapability(
        PostgreSqlConnection connection,
        CancellationToken cancellationToken);
}
