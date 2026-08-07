using Fdw.Services.Connections.Abstractions;

using Fdw.Services.Connections;
using Fdw.Services.Connections.PostgreSql;

using Fdw.Services.Connections.PostgreSql.Discovery;

using Fdw.Services.Connections.PostgreSql.Authentication;

using Fdw.Services.Connections.PostgreSql.Results;

using Fdw.Services.Connections.PostgreSql.Logging;

using Fdw.Services.Connections.PostgreSql.Commands;

using Fdw.Services.Connections.PostgreSql.Validation;


namespace ReferenceConnections.PostgreSql;

/// <summary>
/// Factory interface for creating PostgreSQL connection instances.
/// </summary>
public interface IPostgreSqlConnectionFactory : IConnectionFactory<IGenericConnection, PostgreSqlConnectionConfiguration>
{
}
