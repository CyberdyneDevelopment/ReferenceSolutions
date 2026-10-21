using Fdw.Collections.Attributes;
using Fdw.Services.Connections.Abstractions;
using FdwReferenceConnections.PostgreSql;

namespace FdwReferenceConnections.Script;

/// <summary>The script extension of the PostgreSQL connection (FDW-1447): wraps a <see cref="PostgreSqlConnection"/> in a <see cref="PostgreSqlScriptConnection"/>.</summary>
[TypeOption(typeof(ScriptedConnectionTypes), "PostgreSql")]
public sealed class PostgreSqlScriptConnectionType : ScriptedConnectionTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="PostgreSqlScriptConnectionType"/> class.</summary>
    public PostgreSqlScriptConnectionType() : base("PostgreSql")
    {
    }

    /// <inheritdoc/>
    public override IScriptedDataConnection? Adapt(IDataConnection connection) =>
        connection is PostgreSqlConnection postgres ? new PostgreSqlScriptConnection(postgres) : null;
}
