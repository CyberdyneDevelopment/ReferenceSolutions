using Fdw.Results;
using Fdw.Services.Credentials.Abstractions;
using Fdw.Services.Credentials.Logging;
using Fdw.Services.DataVault.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Credentials.Sql.Configuration;
using Fdw.Services;

namespace FdwReferenceCredentials.Sql;

/// <summary>
/// Factory for creating <see cref="SqlCredentialService"/> instances.
/// </summary>
/// <remarks>
/// Builds from a <see cref="SqlCredentialServiceConfiguration"/> implementation record; the base
/// rejects any other configuration type before this factory sees it. A missing configuration is a
/// failure result — no silent fallbacks.
/// </remarks>
public sealed class SqlCredentialServiceFactory
    : ServiceFactoryBase<ICredentialService, SqlCredentialServiceConfiguration>, ISqlCredentialServiceFactory
{
    private readonly IDataVaultProvider _vaultProvider;
    private readonly ILoggerFactory? _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlCredentialServiceFactory"/> class.
    /// </summary>
    /// <param name="vaultProvider">Provider for resolving credential vaults by name.</param>
    /// <param name="loggerFactory">Logger factory for creating typed loggers for service instances.</param>
    public SqlCredentialServiceFactory(
        IDataVaultProvider vaultProvider,
        ILoggerFactory? loggerFactory = null)
    {
        _vaultProvider = vaultProvider ?? throw new System.ArgumentNullException(nameof(vaultProvider));
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc />
    public override IGenericResult<ICredentialService> Create(SqlCredentialServiceConfiguration configuration)
    {
        if (configuration is null)
            return GenericResult<ICredentialService>.Failure(
                CredentialServiceLog.FactoryConfigurationInvalid(
                    _loggerFactory?.CreateLogger<SqlCredentialServiceFactory>()
                        ?? NullLogger<SqlCredentialServiceFactory>.Instance,
                    "(null)"));

        return GenericResult<ICredentialService>.Success(new SqlCredentialService(
            configuration,
            _vaultProvider,
            _loggerFactory?.CreateLogger<SqlCredentialService>()));
    }
}
