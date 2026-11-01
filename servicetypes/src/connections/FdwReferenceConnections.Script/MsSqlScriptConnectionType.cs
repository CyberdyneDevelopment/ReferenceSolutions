using Fdw.Collections.Attributes;
using Fdw.Services.Connections.Abstractions;
using FdwReferenceConnections.MsSql;

namespace FdwReferenceConnections.Script;

/// <summary>The script extension of the SQL Server connection (FDW-1447): wraps a <see cref="MsSqlConnection"/> in a <see cref="MsSqlScriptConnection"/>.</summary>
[TypeOption(typeof(ScriptedConnectionTypes), "MsSql")]
public sealed class MsSqlScriptConnectionType : ScriptedConnectionTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="MsSqlScriptConnectionType"/> class.</summary>
    public MsSqlScriptConnectionType() : base("MsSql")
    {
    }

    /// <inheritdoc/>
    public override IScriptedDataConnection? Adapt(IDataConnection connection) =>
        connection is MsSqlConnection sqlServer ? new MsSqlScriptConnection(sqlServer) : null;
}
