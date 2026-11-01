using Fdw.Services.Abstractions;
using Fdw.Services.Connections.Abstractions;

namespace FdwReferenceConnections.Sqlite;

/// <summary>The SqliteConnection implementation of its domain.</summary>
public interface ISqliteConnectionProvider : IImplementationServiceProvider<IGenericConnection, IConnectionImplementationConfiguration>
{
}
