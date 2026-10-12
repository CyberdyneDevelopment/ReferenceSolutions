using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.MsSql;


namespace FdwReferenceConnections.MsSql;

/// <summary>
/// Factory interface for creating Microsoft SQL Server connection instances.
/// Uses the flat <see cref="MsSqlConnectionConfiguration"/> pattern.
/// </summary>
/// <remarks>
/// <para>
/// This interface inherits from <see cref="IConnectionFactory{TConnection, TConfiguration}"/>
/// with <see cref="IGenericConnection"/> as the connection type and
/// <see cref="MsSqlConnectionConfiguration"/> as the configuration type.
/// </para>
/// <para>
/// The base interface provides:
/// - Get(MsSqlConnectionConfiguration)
/// - Get(IGenericConfiguration)
/// - Get methods from IServiceFactory hierarchy
/// </para>
/// </remarks>
public interface IMsSqlConnectionFactory : IConnectionFactory<IGenericConnection, MsSqlConnectionConfiguration>
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
    IGenericResult<IGenericConnection> Create(MsSqlConnectionConfiguration configuration, string? resolvedSecret);

    // Inherited from base interface:
    // IGenericResult<IGenericConnection> Get(MsSqlConnectionConfiguration configuration);
    // IGenericResult<IGenericConnection> Get(IGenericConfiguration configuration);
}
