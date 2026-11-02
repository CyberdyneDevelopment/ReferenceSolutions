using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Results;
using Fdw.Services;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.MsSql;
using Fdw.Services.Connections.MsSql.Authentication;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;

namespace FdwReferenceConnections.MsSql;

/// <summary>
/// Builds MsSql connections, resolving any secret the connection's authentication type declares
/// before handing the value to the factory.
/// </summary>
/// <remarks>
/// Why this layer exists: the authentication type decides whether a secret is needed, which manager
/// holds it and under which key — those are its own properties. Fetching it is a different job, and
/// it is asynchronous. An implementation provider may know its implementation, so it can ask
/// <see cref="MsSqlAuthenticationTypes"/> directly; the domain provider may not, which is why the
/// fetching cannot live there, and the factory must not, which is why it is no longer asynchronous.
/// </remarks>
public sealed class MsSqlConnectionProvider
    : ImplementationServiceProviderBase<IGenericConnection, IConnectionImplementationConfiguration>,
      IMsSqlConnectionProvider
{
    private readonly IMsSqlConnectionFactory _factory;
    private readonly ISecretManagerProvider _secretManagerProvider;
    private readonly ILogger<MsSqlConnectionProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlConnectionProvider"/> class.
    /// </summary>
    /// <param name="factory">Builds the connection once everything it needs is in hand.</param>
    /// <param name="secretManagerProvider">Resolves the manager the connection's authentication type names.</param>
    /// <param name="logger">The logger for this provider.</param>
    public MsSqlConnectionProvider(
        IMsSqlConnectionFactory factory,
        ISecretManagerProvider secretManagerProvider,
        ILogger<MsSqlConnectionProvider>? logger = null)
    {
        _factory = factory;
        _secretManagerProvider = secretManagerProvider;
        _logger = logger ?? NullLogger<MsSqlConnectionProvider>.Instance;
    }

    /// <inheritdoc />
    public override async Task<IGenericResult<IGenericConnection>> Create(
        IConnectionImplementationConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        if (configuration is not MsSqlConnectionConfiguration typed)
            return _factory.Create(configuration);

        // No null or wrong-type check here: the base narrows what the domain dispatched and fails
        // loud naming both types, so nothing but a usable configuration reaches this method.
        MsSqlConnectionProviderLogger.TraceResolving(_logger, typed.Name);

        // Cheap fast path first: RequiresSecret is a constructor-set fact about the authentication
        // TYPE, so a method that never carries a password (ManagedIdentity, WindowsAuth, ...) skips
        // the values-dependent RequiredSecret(values) call entirely.
        var authType = MsSqlAuthenticationTypes.ByName(typed.AuthenticationType);
        if (!authType.RequiresSecret)
        {
            MsSqlConnectionProviderLogger.TraceNoSecretRequired(_logger, typed.Name, typed.AuthenticationType!);
            return _factory.Create(typed, resolvedSecret: null);
        }

        // The authentication type answers what it needs; this provider never reads SecretKeyName or
        // SecretManagerName itself, because it cannot see the whole of that question.
        var requirement = authType
            .RequiredSecret(new System.Collections.Generic.Dictionary<string, string?>(typed.AdditionalProperties ?? new System.Collections.Generic.Dictionary<string, string?>(), System.StringComparer.OrdinalIgnoreCase));
        if (!requirement.IsSuccess)
            return requirement.ToNewResult<IGenericConnection>();

        // Nothing to fetch: most authentication types carry no password at all.
        if (requirement.Value.IsEmpty)
        {
            MsSqlConnectionProviderLogger.TraceNoSecretRequired(
                _logger, typed.Name, typed.AuthenticationType!);
            return _factory.Create(typed, resolvedSecret: null);
        }

        MsSqlConnectionProviderLogger.TraceResolvingSecret(
            _logger, typed.Name, requirement.Value.ManagerName);

        var manager = await _secretManagerProvider
            .Get(requirement.Value.ManagerName, cancellationToken).ConfigureAwait(false);
        if (!manager.IsSuccess || manager.Value is null)
            return manager.ToNewResult<IGenericConnection>();

        // NO FALLBACKS: the provider was asked for a manager by name; verify what came back actually
        // carries that name rather than trusting it silently, or a misconfigured provider could read
        // this connection's secret out of a store it never declared.
        if (!string.Equals(manager.Value.Name, requirement.Value.ManagerName, System.StringComparison.Ordinal))
            return GenericResult<IGenericConnection>.Failure(
                MsSqlConnectionProviderLogger.SecretManagerMismatch(
                    _logger, typed.Name, requirement.Value.ManagerName, manager.Value.Name));

        var secret = await manager.Value
            .Execute(GetSecretManagerCommand.Latest(null, requirement.Value.KeyName), cancellationToken)
            .ConfigureAwait(false);
        if (!secret.IsSuccess || secret.Value is null)
            return secret.ToNewResult<IGenericConnection>();

        return _factory.Create(typed, secret.Value.GetStringValue());
    }
}
