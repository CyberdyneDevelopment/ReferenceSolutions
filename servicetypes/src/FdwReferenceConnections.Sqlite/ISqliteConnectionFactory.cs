using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Sqlite;

namespace FdwReferenceConnections.Sqlite;

/// <summary>
/// Factory interface for creating SQLite connection instances.
/// </summary>
public interface ISqliteConnectionFactory : IConnectionFactory<IGenericConnection, SqliteConnectionConfiguration>
{
    /// <summary>Creates the connection from an already-fetched secret.</summary>
    /// <param name="configuration">The connection configuration.</param>
    /// <param name="resolvedSecret">The encryption key this connection declared, or null for None auth.</param>
    /// <returns>The connection, or a structured failure.</returns>
    /// <remarks>
    /// Declared here and not on <see cref="IConnectionFactory"/>: what has to be fetched first is
    /// implementation-specific. Fetching is the implementation provider's job, which is what keeps
    /// every Create on this factory synchronous.
    /// </remarks>
    IGenericResult<IGenericConnection> Create(SqliteConnectionConfiguration configuration, string? resolvedSecret);
}
