using Fdw.Collections;
using Npgsql;

namespace FdwReferenceConnections.PostgreSql.SslModes;

/// <summary>
/// How a PostgreSQL connection uses TLS (FDW-4153). A connection states the option's <c>Name</c> as its SslMode; nothing parses an enum to
/// find it.
/// </summary>
public interface IPostgreSqlSslMode : ITypeOption<int, IPostgreSqlSslMode>
{
    /// <summary>Gets the Npgsql <see cref="SslMode"/> this option is, used only where the connection string is built.</summary>
    SslMode NpgsqlMode { get; }
}
