using Fdw.Collections.Attributes;
using Npgsql;

namespace FdwReferenceConnections.PostgreSql.SslModes;

/// <summary>Requires TLS, a trusted server certificate and a host name that matches it.</summary>
[TypeOption(typeof(PostgreSqlSslModes), "VerifyFull")]
public sealed class VerifyFullSslMode : PostgreSqlSslModeBase
{
    /// <summary>Initializes a new instance of the <see cref="VerifyFullSslMode"/> class.</summary>
    public VerifyFullSslMode()
        : base(6, "VerifyFull", SslMode.VerifyFull)
    {
    }
}
