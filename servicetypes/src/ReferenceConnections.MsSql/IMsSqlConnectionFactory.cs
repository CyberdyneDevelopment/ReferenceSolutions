using Fdw.Configuration;
using Fdw.Services.Connections.Abstractions;

using Fdw.Services.Connections;
using Fdw.Services.Connections.MsSql;

using Fdw.Services.Connections.MsSql.Discovery;

using Fdw.Services.Connections.MsSql.Authentication;

using Fdw.Services.Connections.MsSql.Limits;

using Fdw.Services.Connections.MsSql.ErrorHandlers;

using ReferenceConnections.MsSql.Mapping;

using Fdw.Services.Connections.MsSql.Messages;

using Fdw.Services.Connections.MsSql.Results;

using Fdw.Services.Connections.MsSql.Logging;

using Fdw.Services.Connections.MsSql.Commands;

using Fdw.Services.Connections.MsSql.Validation;



namespace ReferenceConnections.MsSql;

/// <summary>
/// Factory interface for creating Microsoft SQL Server connection instances.
/// Uses the flat <see cref="MsSqlConnectionConfiguration"/> pattern.
/// </summary>
/// <remarks>
/// <para>
/// This interface inherits from <see cref="IConnectionFactory{TConnection, TConfiguration}"/>
/// with <see cref="IGenericConnection"/> as the connection type and
/// <see cref="MsSqlConnectionConfiguration"/> as the configuration type.
/// </para>
/// <para>
/// The base interface provides:
/// - Get(MsSqlConnectionConfiguration)
/// - Get(IGenericConfiguration)
/// - Get methods from IServiceFactory hierarchy
/// </para>
/// </remarks>
public interface IMsSqlConnectionFactory : IConnectionFactory<IGenericConnection, MsSqlConnectionConfiguration>
{
    // Inherited from base interface:
    // IGenericResult<IGenericConnection> Get(MsSqlConnectionConfiguration configuration);
    // IGenericResult<IGenericConnection> Get(IGenericConfiguration configuration);
}
