using Fdw.Services.Hosts.Abstractions;
using FdwReferenceHosts.SqlServerEndpoint.Configuration;

namespace FdwReferenceHosts.SqlServerEndpoint;

/// <summary>
/// Creates the SQL Server endpoint host service from its configuration.
/// </summary>
public interface ISqlServerEndpointHostFactory : IHostFactory<SqlServerEndpointHostService, SqlServerEndpointHostConfiguration>
{
}
