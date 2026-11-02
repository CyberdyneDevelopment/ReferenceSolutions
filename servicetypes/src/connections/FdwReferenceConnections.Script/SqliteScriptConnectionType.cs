using Fdw.Collections.Attributes;
using Fdw.Services.Connections.Abstractions;

namespace FdwReferenceConnections.Script;

/// <summary>The script extension of the SQLite connection (FDW-1447): wraps a SQLite connection in a <see cref="SqliteScriptConnection"/>.</summary>
[TypeOption(typeof(ScriptedConnectionTypes), "Sqlite")]
public sealed class SqliteScriptConnectionType : ScriptedConnectionTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="SqliteScriptConnectionType"/> class.</summary>
    public SqliteScriptConnectionType() : base("Sqlite")
    {
    }

    /// <inheritdoc/>
    public override IScriptedDataConnection? Adapt(IDataConnection connection) =>
        connection is FdwReferenceConnections.Sqlite.SqliteConnection sqlite ? new SqliteScriptConnection(sqlite) : null;
}
