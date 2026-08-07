using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Credentials.Abstractions;
using Fdw.Services.Credentials.Logging;
using Fdw.Services.DataVault.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using CoreConfig = Fdw.Services.Credentials;
using Fdw.Services.Credentials;
using Fdw.Services.Credentials.Sql.Configuration;
using Fdw.Services.Credentials.Sql.Options;
using Fdw.Services.Credentials.Sql.Outcomes;
using Fdw.Services.DataVault;
using Fdw.Services.DataVault.Logging;
using Fdw.Services;
using Fdw;

namespace ReferenceCredentials.Sql;

/// <summary>
/// Factory for creating <see cref="SqlCredentialService"/> instances.
/// </summary>
/// <remarks>
/// Validates that the supplied configuration is a <see cref="CoreConfig.CredentialServiceConfiguration"/>
/// header with a non-null typed body before constructing the service. Returns a failure result if
/// the configuration is missing or malformed — no silent fallbacks.
/// </remarks>
public sealed class SqlCredentialServiceFactory : ICredentialServiceFactory<ICredentialService, CoreConfig.CredentialServiceConfiguration>
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
    public IGenericResult<ICredentialService> Create(CoreConfig.CredentialServiceConfiguration configuration)
    {
        var logger = _loggerFactory?.CreateLogger<SqlCredentialServiceFactory>()
            ?? NullLogger<SqlCredentialServiceFactory>.Instance;

        if (configuration is null)
            return GenericResult<ICredentialService>.Failure(
                CredentialServiceLog.FactoryConfigurationInvalid(logger, "(null)"));

        if (configuration.Configuration is null)
            return GenericResult<ICredentialService>.Failure(
                CredentialServiceLog.FactoryConfigurationInvalid(logger, configuration.Name));

        return GenericResult<ICredentialService>.Success(new SqlCredentialService(
            configuration,
            _vaultProvider,
            _loggerFactory?.CreateLogger<SqlCredentialService>()));
    }

    /// <inheritdoc />
    public IGenericResult<ICredentialService> Create(IGenericConfiguration configuration)
    {
        var logger = _loggerFactory?.CreateLogger<SqlCredentialServiceFactory>()
            ?? NullLogger<SqlCredentialServiceFactory>.Instance;

        if (configuration is not CoreConfig.CredentialServiceConfiguration serviceConfig)
            return GenericResult<ICredentialService>.Failure(
                CredentialServiceLog.FactoryConfigurationInvalid(logger, configuration?.GetType().Name ?? "(null)"));

        return Create(serviceConfig);
    }

    /// <inheritdoc />
    IGenericResult<T> IServiceFactory.Create<T>(IGenericConfiguration configuration)
    {
        var result = Create(configuration);
        if (!result.IsSuccess || result.Value is null)
            return result.ToNewResult<T>();

        if (result.Value is T typed)
            return GenericResult<T>.Success(typed);

        var logger = _loggerFactory?.CreateLogger<SqlCredentialServiceFactory>()
            ?? NullLogger<SqlCredentialServiceFactory>.Instance;
        return GenericResult<T>.Failure(
            CredentialServiceLog.FactoryConfigurationInvalid(logger, typeof(T).Name));
    }

    // Why: explicit IServiceFactory.Create return type must be IGenericResult<IGenericService>
    // to satisfy the non-generic IServiceFactory interface contract.
    IGenericResult<IGenericService> IServiceFactory.Create(IGenericConfiguration configuration)
    {
        var result = ((IServiceFactory<ICredentialService>)this).Create(configuration);
        if (!result.IsSuccess || result.Value is null)
            return result.ToNewResult<IGenericService>();

        return GenericResult<IGenericService>.Success(result.Value);
    }
}
