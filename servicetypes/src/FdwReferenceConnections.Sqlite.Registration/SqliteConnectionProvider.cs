using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Sqlite.Authentication;
using Fdw.Services.Connections.Sqlite;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services;

namespace FdwReferenceConnections.Sqlite;

/// <summary>
/// Builds SQLite connections, resolving the encryption key the connection's authentication type
/// declares (EncryptionKey) before handing the value to the factory. None needs nothing fetched.
/// </summary>
/// <remarks>
/// Mirrors MsSqlConnectionProvider: the authentication type decides whether a secret is needed, which
/// store holds it and under which key — those are its own properties. Fetching it is a different,
/// asynchronous job that belongs here, not in the factory.
/// </remarks>
public sealed class SqliteConnectionProvider : ImplementationServiceProviderBase<IGenericConnection, IConnectionImplementationConfiguration>, ISqliteConnectionProvider
{
    private readonly ISqliteConnectionFactory _factory;
    private readonly ISecretManagerProvider _secretManagerProvider;
    private readonly ILogger<SqliteConnectionProvider> _logger;

    /// <summary>Initializes a new instance of the <see cref="SqliteConnectionProvider"/> class.</summary>
    /// <param name="factory">Builds the connection once everything it needs is in hand.</param>
    /// <param name="secretManagerProvider">Resolves the store the authentication type names.</param>
    /// <param name="logger">The logger for this provider.</param>
    public SqliteConnectionProvider(
        ISqliteConnectionFactory factory,
        ISecretManagerProvider secretManagerProvider,
        ILogger<SqliteConnectionProvider>? logger = null)
    {
        _factory = factory;
        _secretManagerProvider = secretManagerProvider;
        _logger = logger ?? NullLogger<SqliteConnectionProvider>.Instance;
    }

    /// <inheritdoc />
    public override async Task<IGenericResult<IGenericConnection>> Create(
        IConnectionImplementationConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        if (configuration is not SqliteConnectionConfiguration typed)
            return _factory.Create(configuration);

        // Cheap fast path first: RequiresSecret is a constructor-set fact about the authentication
        // TYPE, so None skips the values-dependent RequiredSecret(values) call entirely.
        var authType = SqliteAuthenticationTypes.ByName(typed.AuthenticationType ?? string.Empty);
        if (!authType.RequiresSecret)
            return _factory.Create(typed, resolvedSecret: null);

        // The auth type has no logger of its own (it's a stateless TypeOption), so a failure it
        // reports is only ever logged here, where one is actually available.
        var requirement = authType
            .RequiredSecret(new Dictionary<string, string?>(typed.AdditionalProperties ?? new Dictionary<string, string?>(), StringComparer.OrdinalIgnoreCase));
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
