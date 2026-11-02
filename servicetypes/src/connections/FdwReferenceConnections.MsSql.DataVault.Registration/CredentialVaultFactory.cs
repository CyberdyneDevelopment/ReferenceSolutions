using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.DataVault;
using Fdw.Services.DataVault.Abstractions;
using Fdw.Services.DataVault.Logging;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceConnections.MsSql.DataVault.Registration;

/// <summary>
/// Factory for <see cref="CredentialVault"/>. Resolves its own connection and pepper from the vault's
/// implementation configuration — constructor-injected <see cref="IDeclaredConnectionProvider"/> and
/// <see cref="ISecretManagerProvider"/>, exactly mirroring how <c>MsSqlConnectionFactory</c> takes
/// <c>ISecretManagerProvider</c> as its own constructor dependency. Resolution is genuinely
/// asynchronous (two provider round-trips), so this factory implements
/// <see cref="IAsyncServiceFactory{TService}"/> rather than blocking inside a sync Create.
/// </summary>
public sealed class CredentialVaultFactory
    : ServiceFactoryBase<IDataVault, SqlDataVaultConfiguration>, ICredentialVaultFactory
{
    private readonly ILoggerFactory? _loggerFactory;
    private readonly IDeclaredConnectionProvider _connectionProvider;
    private readonly ISecretManagerProvider _secretManagerProvider;
    private readonly IAuthenticationContextAccessor _authContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="CredentialVaultFactory"/> class.
    /// </summary>
    /// <param name="connectionProvider">
    /// Builds the vault's single data connection by name: a connection the host declares in its
    /// <c>configurationSchema.json</c>, so the vault's store is never a ConfigurationDb row a user route can name (FDW-1084).
    /// </param>
    /// <param name="secretManagerProvider">Resolves the secret manager that holds the pepper.</param>
    /// <param name="authContextAccessor">Elevated to system context while the connection and pepper are read.</param>
    /// <param name="loggerFactory">Logger factory for creating typed loggers.</param>
    public CredentialVaultFactory(
        IDeclaredConnectionProvider connectionProvider,
        ISecretManagerProvider secretManagerProvider,
        IAuthenticationContextAccessor authContextAccessor,
        ILoggerFactory? loggerFactory = null)
    {
        _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        _secretManagerProvider = secretManagerProvider ?? throw new ArgumentNullException(nameof(secretManagerProvider));
        _authContextAccessor = authContextAccessor ?? throw new ArgumentNullException(nameof(authContextAccessor));
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IDataVault>> Create(IGenericConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var logger = _loggerFactory?.CreateLogger<CredentialVaultFactory>()
            ?? NullLogger<CredentialVaultFactory>.Instance;

        if (configuration is not SqlDataVaultConfiguration body)
            return GenericResult<IDataVault>.Failure(DataVaultLog.FactoryConfigurationInvalid(logger, Identify(configuration as IDataVaultImplementationConfiguration)));

        var vaultName = Identify(body);

        var resolved = await ResolveConnectionAndPepper(body, vaultName, cancellationToken).ConfigureAwait(false);
        if (!resolved.IsSuccess)
            return resolved.ToNewResult<IDataVault>();

        var vault = new CredentialVault(
            vaultName,
            resolved.Value.Connection,
            resolved.Value.Pepper,
            _loggerFactory?.CreateLogger<CredentialVault>());

        DataVaultLog.VaultInitialized(logger, vaultName, body.ConnectionName);
        return GenericResult<IDataVault>.Success(vault);
    }

    // Why: moved verbatim from DataVaultProvider.ResolveConnectionAndPepper — same validation order,
    // same DataVaultLog events. The provider used to resolve this once and pass the result in; now the
    // factory that needs it resolves its own dependencies, like every other factory in the codebase.
    private async Task<IGenericResult<(IDataConnection Connection, byte[] Pepper)>> ResolveConnectionAndPepper(
        SqlDataVaultConfiguration body, string vaultName, CancellationToken cancellationToken)
    {
        var logger = _loggerFactory?.CreateLogger<CredentialVaultFactory>()
            ?? NullLogger<CredentialVaultFactory>.Instance;

        if (string.IsNullOrWhiteSpace(body.ConnectionName))
            return GenericResult<(IDataConnection, byte[])>.Failure(DataVaultLog.ConnectionNameMissing(logger, vaultName));
        if (string.IsNullOrWhiteSpace(body.SecretManagerName))
            return GenericResult<(IDataConnection, byte[])>.Failure(DataVaultLog.SecretManagerNameMissing(logger, vaultName));
        if (string.IsNullOrWhiteSpace(body.PepperSecretName))
            return GenericResult<(IDataConnection, byte[])>.Failure(DataVaultLog.PepperSecretNameMissing(logger, vaultName));

        // Why system context: the vault's connection (AuthDb, declared by the host) and its pepper are platform
        // secrets, and the pepper is read through the secret manager as the platform. Whoever
        // triggers this -- a login, a user changing their own password, a PAT being minted -- has
        // already been authorized for THAT operation by the service or endpoint above; reading the
        // vault's own plumbing is the platform's job, never the caller's.
        using var systemScope = new SystemAuthenticationContextScope(_authContextAccessor);

        var connectionResult = await _connectionProvider.Get(body.ConnectionName, cancellationToken).ConfigureAwait(false);
        if (!connectionResult.IsSuccess || connectionResult.Value is null)
            return GenericResult<(IDataConnection, byte[])>.Failure(DataVaultLog.ConnectionResolveFailed(logger, vaultName, body.ConnectionName));

        var managerResult = await _secretManagerProvider.Get(body.SecretManagerName, cancellationToken).ConfigureAwait(false);
        if (!managerResult.IsSuccess || managerResult.Value is null)
            return GenericResult<(IDataConnection, byte[])>.Failure(DataVaultLog.SecretManagerResolveFailed(logger, vaultName, body.SecretManagerName));

        var secretResult = await managerResult.Value
            .Execute(GetSecretManagerCommand.Latest(container: null, secretKey: body.PepperSecretName), cancellationToken)
            .ConfigureAwait(false);
        if (!secretResult.IsSuccess || secretResult.Value is not SecretValue secret)
            return GenericResult<(IDataConnection, byte[])>.Failure(DataVaultLog.PepperReadFailed(logger, vaultName, body.SecretManagerName, body.PepperSecretName));

        byte[] pepper;
        using (secret)
        {
            pepper = secret.IsBinary
                ? secret.GetBinaryValue()
                : Encoding.UTF8.GetBytes(secret.GetStringValue());
        }

        if (pepper.Length == 0)
            return GenericResult<(IDataConnection, byte[])>.Failure(DataVaultLog.PepperEmpty(logger, vaultName));

        return GenericResult<(IDataConnection, byte[])>.Success((connectionResult.Value, pepper));
    }

    // Why: config-only (synchronous) creation is NOT supported for vaults — resolving the connection
    // and pepper is genuinely async, so the real work happens in the IAsyncServiceFactory overload
    // above, which PlatformServiceProviderBase prefers when a factory implements it. This member
    // exists solely because ServiceFactoryBase declares it; it fails loud (never a fallback), and the
    // base's erased and generic Create inherit that failure.

    /// <inheritdoc />
    public override IGenericResult<IDataVault> Create(SqlDataVaultConfiguration configuration)
        => GenericResult<IDataVault>.Failure(DataVaultLog.FactoryConfigurationInvalid(
            _loggerFactory?.CreateLogger<CredentialVaultFactory>() ?? NullLogger<CredentialVaultFactory>.Instance,
            Identify(configuration)));

    // Why: the typed body has no independent name (SqlDataVaultConfiguration.Name is hard-wired
    // empty — it is identified by DataVaultId, the FK to its sec.DataVault domain row). Same pattern
    // as MsSqlConnectionFactory.CreateCore's `typedBody.ConnectionId.ToString()`.
    private static string Identify(IDataVaultImplementationConfiguration? configuration)
        => configuration?.Id.ToString() ?? "(null)";
}
