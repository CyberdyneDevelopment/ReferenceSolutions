using Fdw.Services.Multitenancy;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace FdwReferenceMultitenancy.Sql;

/// <summary>Supplies the SqlTenantProvider implementation's own configuration.</summary>
public sealed class SqlTenantProviderImplementationConfigurationProvider
    : ImplementationProviderBase<SqlTenantConfiguration, IMultitenancyImplementationConfiguration>,
      ISqlTenantProviderImplementationConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="SqlTenantProviderImplementationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The store this provider's rows live in -- passed by its registration.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    public SqlTenantProviderImplementationConfigurationProvider(
        ILogger<SqlTenantProviderImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
