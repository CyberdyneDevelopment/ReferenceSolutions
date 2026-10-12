using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.PostgreSql;
using Fdw.Services.Connections.PostgreSql.Authentication;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceConnections.PostgreSql;

/// <summary>
/// Builds PostgreSQL connections, resolving any secret the connection's authentication type
/// declares before handing the value to the factory.
/// </summary>
/// <remarks>
/// The authentication type decides whether a secret is needed, which store holds it and under which
/// key — those are its own properties. Fetching it is a different job and it is asynchronous, so it
/// happens here. An implementation provider may know its implementation, so it can ask
/// <see cref="PostgreSqlAuthenticationTypes"/> directly; the domain provider may not.
/// </remarks>
public sealed class PostgreSqlConnectionProvider
    : ImplementationServiceProviderBase<IGenericConnection, IConnectionImplementationConfiguration>,
      IPostgreSqlConnectionProvider
{
    private readonly IPostgreSqlConnectionFactory _factory;
    private readonly ISecretManagerProvider _secretManagerProvider;
    private readonly ILogger<PostgreSqlConnectionProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlConnectionProvider"/> class.
    /// </summary>
    /// <param name="factory">Builds the connection once everything it needs is in hand.</param>
    /// <param name="secretManagerProvider">Resolves the store the authentication type names.</param>
    /// <param name="logger">The logger for this provider.</param>
    public PostgreSqlConnectionProvider(
        IPostgreSqlConnectionFactory factory,
        ISecretManagerProvider secretManagerProvider,
        ILogger<PostgreSqlConnectionProvider>? logger = null)
    {
        _factory = factory;
        _secretManagerProvider = secretManagerProvider;
        _logger = logger ?? NullLogger<PostgreSqlConnectionProvider>.Instance;
    }

    /// <inheritdoc />
    public override async Task<IGenericResult<IGenericConnection>> Create(
        IConnectionImplementationConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        if (configuration is not PostgreSqlConnectionConfiguration typed)
            return _factory.Create(configuration);

        // Cheap fast path first: RequiresSecret is a constructor-set fact about the authentication
        // TYPE, so None skips the values-dependent RequiredSecret(values) call entirely.
        var authType = PostgreSqlAuthenticationTypes.ByName(typed.AuthenticationType);
        if (!authType.RequiresSecret)
            return _factory.Create(typed, resolvedSecret: null);

        // The auth type has no logger of its own (it's a stateless TypeOption), so a failure it
        // reports is only ever logged here, where one is actually available.
        var requirement = authType
            .RequiredSecret(new System.Collections.Generic.Dictionary<string, string?>(typed.AdditionalProperties ?? new System.Collections.Generic.Dictionary<string, string?>(), System.StringComparer.OrdinalIgnoreCase));
        if (!requirement.IsSuccess)
        {
            return requirement.Code is { } code
                ? GenericResult<IGenericConnection>.Failure(code, _logger, requirement.Details)
                : requirement.ToNewResult<IGenericConnection>();
        }

        if (requirement.Value.IsEmpty)
            return _factory.Create(typed, resolvedSecret: null);

        var manager = await _secretManagerProvider
            .Get(requirement.Value.ManagerName, cancellationToken).ConfigureAwait(false);
        if (!manager.IsSuccess || manager.Value is null)
            return manager.ToNewResult<IGenericConnection>();

        var secret = await manager.Value
            .Execute(GetSecretManagerCommand.Latest(null, requirement.Value.KeyName), cancellationToken)
            .ConfigureAwait(false);
        if (!secret.IsSuccess || secret.Value is null)
            return secret.ToNewResult<IGenericConnection>();

        return _factory.Create(typed, secret.Value.GetStringValue());
    }
}
