using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Hosts.Abstractions;
using FdwReferenceHosts.SqlServerEndpoint.Configuration;
using Microsoft.Extensions.Logging;

namespace FdwReferenceHosts.SqlServerEndpoint;

/// <summary>
/// Reads the host implementation rows of the SQL Server endpoint domain.
/// </summary>
public sealed class SqlServerEndpointHostConfigurationProvider
    : ImplementationProviderBase<SqlServerEndpointHostConfiguration, ISqlServerEndpointImplementationConfiguration>,
      ISqlServerEndpointHostConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="SqlServerEndpointHostConfigurationProvider"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="gatewayProvider">Provides the configuration gateway rows are read through.</param>
    /// <param name="dataStoreName">The store the rows are read from.</param>
    /// <param name="pathName">The path the container sits in.</param>
    /// <param name="containerName">The container.</param>
    public SqlServerEndpointHostConfigurationProvider(
        ILogger<SqlServerEndpointHostConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
