using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.PostgreSql;


namespace FdwReferenceConnections.PostgreSql;

/// <summary>
/// Factory interface for creating PostgreSQL connection instances.
/// </summary>
public interface IPostgreSqlConnectionFactory : IConnectionFactory<IGenericConnection, PostgreSqlConnectionConfiguration>
{
    /// <summary>Creates the connection from an already-fetched secret.</summary>
    /// <param name="configuration">The connection configuration.</param>
    /// <param name="resolvedSecret">The value read for this connection's declared secret, or null.</param>
    /// <returns>The connection, or a structured failure.</returns>
    /// <remarks>
    /// Declared here and not on <see cref="IConnectionFactory"/>: what has to be fetched first is
    /// implementation-specific, and a domain-wide contract cannot name it without being wrong for
    /// some other implementation. Fetching is this implementation's provider's job, which is what
    /// keeps every Create on this factory synchronous.
    /// </remarks>
    IGenericResult<IGenericConnection> Create(PostgreSqlConnectionConfiguration configuration, string? resolvedSecret);

}
