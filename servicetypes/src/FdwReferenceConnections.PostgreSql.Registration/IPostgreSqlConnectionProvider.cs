using Fdw.Services.Abstractions;
using Fdw.Services.Connections.Abstractions;

namespace FdwReferenceConnections.PostgreSql;

/// <summary>The PostgreSqlConnection implementation of its domain.</summary>
public interface IPostgreSqlConnectionProvider : IImplementationServiceProvider<IGenericConnection, IConnectionImplementationConfiguration>
{
}
