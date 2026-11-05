using Fdw.Collections.Attributes;
using Npgsql;

namespace FdwReferenceConnections.PostgreSql.SslModes;

/// <summary>Uses TLS when the server supports it.</summary>
[TypeOption(typeof(PostgreSqlSslModes), "Prefer")]
public sealed class PreferSslMode : PostgreSqlSslModeBase
{
    /// <summary>Initializes a new instance of the <see cref="PreferSslMode"/> class.</summary>
    public PreferSslMode()
        : base(3, "Prefer", SslMode.Prefer)
    {
    }
}
