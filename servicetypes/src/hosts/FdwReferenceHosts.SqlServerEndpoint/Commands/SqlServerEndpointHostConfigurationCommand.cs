using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using FdwReferenceHosts.SqlServerEndpoint.Configuration;

namespace FdwReferenceHosts.SqlServerEndpoint.Commands;

/// <summary>ConfigurationCommands TypeOption for the host SQL Server endpoint records.</summary>
[TypeOption(typeof(ConfigurationCommands), "HostSqlServerEndpointConfiguration")]
public sealed class SqlServerEndpointHostConfigurationCommand : ConfigurationCommandBase<SqlServerEndpointHostConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="SqlServerEndpointHostConfigurationCommand"/> class.</summary>
    public SqlServerEndpointHostConfigurationCommand()
        : base("HostSqlServerEndpointConfiguration")
    {
    }
}
