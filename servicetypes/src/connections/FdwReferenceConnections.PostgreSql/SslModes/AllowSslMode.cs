using Fdw.Collections.Attributes;
using Npgsql;

namespace FdwReferenceConnections.PostgreSql.SslModes;

/// <summary>Uses TLS only when the server requires it.</summary>
[TypeOption(typeof(PostgreSqlSslModes), "Allow")]
public sealed class AllowSslMode : PostgreSqlSslModeBase
{
    /// <summary>Initializes a new instance of the <see cref="AllowSslMode"/> class.</summary>
    public AllowSslMode()
        : base(2, "Allow", SslMode.Allow)
    {
    }
}
