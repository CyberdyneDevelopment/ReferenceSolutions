using Fdw.Services.Configuration;
using Fdw.Services.Credentials.Sql.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

using Fdw.Services.Credentials.Abstractions;
namespace FdwReferenceCredentials.Sql.Registration;

/// <summary>Supplies the SqlCredentialService implementation's own configuration.</summary>
public sealed class SqlCredentialServiceConfigurationProvider
    : ImplementationProviderBase<SqlCredentialServiceConfiguration, ICredentialServiceImplementationConfiguration>,
      ISqlCredentialServiceConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="SqlCredentialServiceConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The store this provider's rows live in -- passed by its registration.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    public SqlCredentialServiceConfigurationProvider(
        ILogger<SqlCredentialServiceConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
