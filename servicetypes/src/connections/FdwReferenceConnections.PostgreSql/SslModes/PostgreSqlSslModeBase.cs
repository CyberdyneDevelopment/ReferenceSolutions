using Fdw.Collections;
using Npgsql;

namespace FdwReferenceConnections.PostgreSql.SslModes;

/// <summary>
/// Base of the <see cref="PostgreSqlSslModes"/> options: each names a mode and carries the Npgsql <see cref="SslMode"/> it is.
/// </summary>
public abstract class PostgreSqlSslModeBase : TypeOptionBase<PostgreSqlSslModeBase>, IPostgreSqlSslMode
{
    /// <summary>Initializes a new instance of the <see cref="PostgreSqlSslModeBase"/> class.</summary>
    /// <param name="id">The option's id, unique in the collection.</param>
    /// <param name="name">The name a connection states as its SslMode.</param>
    /// <param name="npgsqlMode">The Npgsql value this option is.</param>
    protected PostgreSqlSslModeBase(int id, string name, SslMode npgsqlMode)
        : base(id, name)
    {
        NpgsqlMode = npgsqlMode;
    }

    /// <inheritdoc />
    public SslMode NpgsqlMode { get; }
}
