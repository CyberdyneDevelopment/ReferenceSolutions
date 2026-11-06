using Fdw.Collections.Attributes;
using Npgsql;

namespace FdwReferenceConnections.PostgreSql.SslModes;

/// <summary>Requires TLS and a server certificate signed by a trusted authority.</summary>
[TypeOption(typeof(PostgreSqlSslModes), "VerifyCA")]
public sealed class VerifyCASslMode : PostgreSqlSslModeBase
{
    /// <summary>Initializes a new instance of the <see cref="VerifyCASslMode"/> class.</summary>
    public VerifyCASslMode()
        : base(5, "VerifyCA", SslMode.VerifyCA)
    {
    }
}
