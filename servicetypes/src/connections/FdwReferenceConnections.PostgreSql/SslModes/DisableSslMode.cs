using Fdw.Collections.Attributes;
using Npgsql;

namespace FdwReferenceConnections.PostgreSql.SslModes;

/// <summary>Never uses TLS.</summary>
[TypeOption(typeof(PostgreSqlSslModes), "Disable")]
public sealed class DisableSslMode : PostgreSqlSslModeBase
{
    /// <summary>Initializes a new instance of the <see cref="DisableSslMode"/> class.</summary>
    public DisableSslMode()
        : base(1, "Disable", SslMode.Disable)
    {
    }
}
