using Fdw.Services.Abstractions;
using Fdw.Services.Hosts.Abstractions;

namespace FdwReferenceHosts.SqlServerEndpoint;

/// <summary>
/// Reads the host implementation rows of the SQL Server endpoint domain.
/// </summary>
public interface ISqlServerEndpointHostConfigurationProvider
    : IImplementationConfigurationProvider<ISqlServerEndpointImplementationConfiguration>
{
}
