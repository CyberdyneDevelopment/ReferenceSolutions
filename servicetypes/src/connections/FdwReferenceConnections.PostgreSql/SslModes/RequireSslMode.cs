using Fdw.Collections.Attributes;
using Npgsql;

namespace FdwReferenceConnections.PostgreSql.SslModes;

/// <summary>Requires TLS, without verifying the server certificate.</summary>
[TypeOption(typeof(PostgreSqlSslModes), "Require")]
public sealed class RequireSslMode : PostgreSqlSslModeBase
{
    /// <summary>Initializes a new instance of the <see cref="RequireSslMode"/> class.</summary>
    public RequireSslMode()
        : base(4, "Require", SslMode.Require)
    {
    }
}
