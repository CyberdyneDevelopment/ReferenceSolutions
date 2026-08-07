using Fdw.Configuration;
using Fdw.Services.Connections.Abstractions;

using Fdw.Services.Connections;
using Fdw.Services.Connections.Sqlite;

using Fdw.Services.Connections.Sqlite.Authentication;

using Fdw.Services.Connections.Sqlite.Commands;

using Fdw.Services.Connections.Sqlite.Validation;

namespace ReferenceConnections.Sqlite;

/// <summary>
/// Factory interface for creating SQLite connection instances.
/// </summary>
public interface ISqliteConnectionFactory : IConnectionFactory<IGenericConnection, SqliteConnectionConfiguration>
{
}
