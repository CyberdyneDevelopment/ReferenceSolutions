using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.DataVault;
using Fdw.Services.DataVault.Abstractions;
using Fdw.Services.DataVault.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using ReferenceCredentials.Sql;
using ReferenceConnections.MsSql.DataVault;

namespace ReferenceConnections.MsSql.DataVault.Registration;

/// <summary>
/// Factory for <see cref="CredentialVault"/>. A PURE constructor: it receives the already-resolved
/// connection and pepper from <c>DefaultDataVaultProvider</c> (the only resolution path, once per vault
/// name, in system context) and simply news up the vault. It holds NO providers and resolves nothing.
/// </summary>
public sealed class CredentialVaultFactory : IDataVaultFactory<IDataVault, DataVaultConfiguration>
{
    private readonly ILoggerFactory? _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="CredentialVaultFactory"/> class.
    /// </summary>
    /// <param name="loggerFactory">Logger factory for creating typed loggers.</param>
    public CredentialVaultFactory(ILoggerFactory? loggerFactory = null)
    {
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc />
    public IGenericResult<IDataVault> Create(DataVaultConfiguration configuration, IDataConnection connection, byte[] pepper)
    {
        var logger = _loggerFactory?.CreateLogger<CredentialVaultFactory>()
            ?? NullLogger<CredentialVaultFactory>.Instance;

        if (configuration is null)
            return GenericResult<IDataVault>.Failure(DataVaultLog.FactoryConfigurationInvalid(logger, "(null)"));

        // Why: connection + pepper are resolved by the provider before this call; a null here means the
        // provider contract was violated. Fail loud rather than throw out of a factory.
        if (connection is null || pepper is null)
            return GenericResult<IDataVault>.Failure(DataVaultLog.FactoryConfigurationInvalid(logger, configuration.Name));

        return GenericResult<IDataVault>.Success(new CredentialVault(
            configuration.Name,
            connection,
            pepper,
            _loggerFactory?.CreateLogger<CredentialVault>()));
    }

    // Why: config-only creation is NOT supported for vaults — a vault cannot be built without its
    // resolved connection and pepper, and the ONLY resolution path is DefaultDataVaultProvider's
    // cache-factory, which calls the (config, connection, pepper) overload above. These IServiceFactory
    // members exist solely to satisfy the interface contract; they fail loud (never a fallback).

    /// <inheritdoc />
    public IGenericResult<IDataVault> Create(DataVaultConfiguration configuration)
        => RejectConfigOnly(configuration?.Name);

    /// <inheritdoc />
    public IGenericResult<IDataVault> Create(IGenericConfiguration configuration)
        => RejectConfigOnly((configuration as DataVaultConfiguration)?.Name);

    /// <inheritdoc />
    IGenericResult<T> IServiceFactory.Create<T>(IGenericConfiguration configuration)
    {
        var logger = _loggerFactory?.CreateLogger<CredentialVaultFactory>()
            ?? NullLogger<CredentialVaultFactory>.Instance;
        return GenericResult<T>.Failure(DataVaultLog.FactoryConfigurationInvalid(logger, (configuration as DataVaultConfiguration)?.Name ?? "(null)"));
    }

    // Why: explicit IServiceFactory.Create return type must be IGenericResult<IGenericService>
    // to satisfy the non-generic IServiceFactory interface contract.
    IGenericResult<IGenericService> IServiceFactory.Create(IGenericConfiguration configuration)
    {
        var logger = _loggerFactory?.CreateLogger<CredentialVaultFactory>()
            ?? NullLogger<CredentialVaultFactory>.Instance;
        return GenericResult<IGenericService>.Failure(DataVaultLog.FactoryConfigurationInvalid(logger, (configuration as DataVaultConfiguration)?.Name ?? "(null)"));
    }

    private IGenericResult<IDataVault> RejectConfigOnly(string? name)
    {
        var logger = _loggerFactory?.CreateLogger<CredentialVaultFactory>()
            ?? NullLogger<CredentialVaultFactory>.Instance;
        return GenericResult<IDataVault>.Failure(DataVaultLog.FactoryConfigurationInvalid(logger, name ?? "(null)"));
    }
}
