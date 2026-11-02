using Fdw.Services.Abstractions;

using Fdw.Services.Connections.Abstractions;
namespace FdwReferenceConnections.PostgreSql;

/// <summary>Supplies the PostgreSqlConnection implementation's own configuration.</summary>
public interface IPostgreSqlConnectionConfigurationProvider
    : IImplementationConfigurationProvider<IConnectionImplementationConfiguration>
{
}
