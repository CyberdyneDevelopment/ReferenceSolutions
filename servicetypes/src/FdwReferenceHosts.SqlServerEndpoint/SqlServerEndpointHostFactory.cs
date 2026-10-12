using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services;
using FdwReferenceHosts.SqlServerEndpoint.Configuration;

namespace FdwReferenceHosts.SqlServerEndpoint;

/// <summary>
/// Creates the SQL Server endpoint host service from its configuration.
/// </summary>
public sealed class SqlServerEndpointHostFactory
    : ServiceFactoryBase<SqlServerEndpointHostService, SqlServerEndpointHostConfiguration>, ISqlServerEndpointHostFactory
{
    /// <inheritdoc/>
    public override IGenericResult<SqlServerEndpointHostService> Create(SqlServerEndpointHostConfiguration configuration)
        => Create((IGenericConfiguration)configuration);
}
