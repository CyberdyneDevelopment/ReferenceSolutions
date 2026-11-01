using Fdw.Services.Abstractions;

using Fdw.Services.Connections.Abstractions;
namespace FdwReferenceConnections.Sqlite;

/// <summary>Supplies the SqliteConnection implementation's own configuration.</summary>
public interface ISqliteConnectionConfigurationProvider
    : IImplementationConfigurationProvider<IConnectionImplementationConfiguration>
{
}
