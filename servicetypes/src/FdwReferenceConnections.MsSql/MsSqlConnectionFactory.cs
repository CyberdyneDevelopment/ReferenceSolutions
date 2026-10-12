using Fdw.Services;
using System;
using Fdw.Commands.Data.Abstractions.Logging;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.MsSql.Authentication;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.Connections.MsSql;
using FdwReferenceConnections.MsSql.Logging;

namespace FdwReferenceConnections.MsSql;

/// <summary>
/// Factory for creating Microsoft SQL Server connection instances.
/// </summary>
/// <remarks>
/// This factory accepts <see cref="MsSqlConnectionConfiguration"/> and creates
/// <see cref="MsSqlConnection"/> instances with <see cref="SqlConnection"/> objects.
/// The connection string is built using authentication processors and goes out of scope
/// immediately after creating the SqlConnection for security.
/// </remarks>
public sealed class MsSqlConnectionFactory : ServiceFactoryBase<IGenericConnection, MsSqlConnectionConfiguration>, IMsSqlConnectionFactory
{
    private readonly ILogger<MsSqlConnectionFactory> _logger;
    private readonly ILogger<MsSqlConnection> _connectionLogger;
    // Why the ACCESSOR and not an IAuthenticationContext: this factory is registered Singleton (the
    // three-phase eager-resolve/cache pattern requires it), so any IAuthenticationContext captured
    // here is captured once, at composition time, when no request or execution flow exists — it is
    // null for the life of the process and every connection it builds computes the deny-everywhere
    // plan. The accessor is AsyncLocal-backed, so a Singleton may hold it and still read the CURRENT
    // logical call flow's context. Connections therefore resolve the context through it at plan
    // time, never at construction.
    private readonly IAuthenticationContextAccessor? _authenticationContextAccessor;
    // Why: the factory owns secret resolution. It is a constructor dependency, exactly as
    // HttpConnectionFactory takes IHttpClientFactory — the option registers what it needs in its own
    // Register. Null only on the provider-less constructor below, and that is a state
    // the authentication type decides whether to care about.
    private readonly ISecretManagerProvider? _secretManagerProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlConnectionFactory"/> class that resolves
    /// secrets through the supplied secret-manager provider.
    /// </summary>
    /// <param name="logger">The logger for factory operations.</param>
    /// <param name="connectionLogger">The logger for connection instances.</param>
    /// <param name="secretManagerProvider">The secret-manager provider, resolved by name per connection.</param>
    /// <param name="authenticationContextAccessor">
    /// The ambient accessor connections read the calling principal from at plan time, for RLS
    /// SESSION_CONTEXT injection. Optional: a host whose connection kinds declare no session
    /// contexts never registers one.
    /// </param>
    public MsSqlConnectionFactory(
        ILogger<MsSqlConnectionFactory> logger,
        ILogger<MsSqlConnection> connectionLogger,
        ISecretManagerProvider secretManagerProvider,
        IAuthenticationContextAccessor? authenticationContextAccessor = null)
    {
        _logger = logger;
        _connectionLogger = connectionLogger;
        _secretManagerProvider = secretManagerProvider;
        _authenticationContextAccessor = authenticationContextAccessor;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlConnectionFactory"/> class with no
    /// secret-manager provider.
    /// </summary>
    /// <param name="logger">The logger for factory operations.</param>
    /// <param name="connectionLogger">The logger for connection instances.</param>
    /// <param name="authenticationContextAccessor">
    /// The ambient accessor connections read the calling principal from at plan time, for RLS
    /// SESSION_CONTEXT injection. Optional: a host whose connection kinds declare no session
    /// contexts never registers one.
    /// </param>
    /// <remarks>
    /// For the connection that reaches PlatformConfiguration, which cannot resolve a secret manager by name
    /// because the provider that would do so reads its own configuration out of the database being
    /// opened. That caller supplies a concrete <see cref="ISecretManager"/> to the matching Create
    /// overload instead. Any authentication type that declares no secret-bearing properties works
    /// here unchanged.
    /// </remarks>
    public MsSqlConnectionFactory(
        ILogger<MsSqlConnectionFactory> logger,
        ILogger<MsSqlConnection> connectionLogger,
        IAuthenticationContextAccessor? authenticationContextAccessor = null)
    {
        _logger = logger;
        _connectionLogger = connectionLogger;
        _secretManagerProvider = null;
        _authenticationContextAccessor = authenticationContextAccessor;
    }


    // The factory is handed the implementation configuration the domain provider dispatched to it.
    // Its Name is the domain row's, which this never sees; ConnectionId is the identity it does carry
    // and the one a reader follows back to the named row.
    // The factory builds from the implementation configuration: Server, Database, Port,
    // InstanceName and AuthenticationType all live there.
    private static MsSqlConnectionConfiguration? Unwrap(IGenericConfiguration configuration)
        => configuration as MsSqlConnectionConfiguration;






    /// <inheritdoc />
    /// <remarks>
    /// The authentication type answers this; the factory only relays. It owns SecretManagerName and
    /// SecretKeyName, so a new authentication type changes nothing here.
    /// </remarks>
    public IGenericResult<SecretRequirement> RequiredSecret(IGenericConfiguration configuration)
        => Unwrap(configuration) is { } typed
            ? MsSqlAuthenticationTypes.ByName(typed.AuthenticationType)
                .RequiredSecret(NormalizeAuthValues(typed.AdditionalProperties))
            : GenericResult<SecretRequirement>.Failure(
                MsSqlConnectionFactoryLogger.InvalidConfigurationType(
                    _logger, configuration?.GetType().Name ?? "null"));

    /// <inheritdoc />
    public IGenericResult<IGenericConnection> Create(IGenericConfiguration configuration, string? resolvedSecret)
        => Unwrap(configuration) is { } typed
            ? Create(typed, resolvedSecret)
            : GenericResult<IGenericConnection>.Failure(
                MsSqlConnectionFactoryLogger.InvalidConfigurationType(
                    _logger, configuration?.GetType().Name ?? "null"));

    /// <summary>
    /// Creates a SQL Server connection from the flat configuration, with no secret to apply.
    /// </summary>
    /// <param name="configuration">The connection configuration.</param>
    /// <returns>The connection, or a structured failure.</returns>
    public override IGenericResult<IGenericConnection> Create(MsSqlConnectionConfiguration configuration)
        => Create(configuration, resolvedSecret: null);

    /// <summary>
    /// Creates a SQL Server connection from the flat configuration and an already-fetched secret.
    /// </summary>
    /// <param name="configuration">The connection configuration.</param>
    /// <param name="resolvedSecret">The value read for this connection's declared secret, or null.</param>
    /// <returns>The connection, or a structured failure.</returns>
    /// <remarks>
    /// Synchronous, and every other Create overload lands here. The secret arrives as a value
    /// because fetching it is the implementation provider's job -- this only builds, and building
    /// never needs to await. The authentication type still produces the secret portion of the
    /// connection string, from the value it is handed.
    /// </remarks>
    public IGenericResult<IGenericConnection> Create(
        MsSqlConnectionConfiguration configuration,
        string? resolvedSecret)
    {
        if (configuration is null)
            return GenericResult<IGenericConnection>.Failure(
                MsSqlConnectionFactoryLogger.ConfigurationNull(_logger));

        var name = configuration.Name;
        MsSqlConnectionFactoryLogger.TraceCreateEntry(_logger, name);

        if (string.IsNullOrEmpty(configuration.Server))
            return GenericResult<IGenericConnection>.Failure(
                MsSqlConnectionFactoryLogger.ServerNotSpecified(_logger, name));

        if (string.IsNullOrEmpty(configuration.AuthenticationType))
            return GenericResult<IGenericConnection>.Failure(
                MsSqlConnectionFactoryLogger.AuthenticationTypeNotSpecified(_logger, name));

        // Never defaulted (FDW-1152): how many rows an insert runs serially before it takes the bulk route is stated in the
        // connection's configuration, and a connection that does not state it is refused, naming it.
        if (configuration.MaxSerialInsert <= 0)
            return GenericResult<IGenericConnection>.Failure(DatabaseCommandLog.MaxSerialInsertNotStated(_logger, name));

        // Why: ByName returns the NotFound sentinel for an unrecognized type -- never null. An
        // unknown authentication type is a configuration defect, not "no authentication".
        var auth = MsSqlAuthenticationTypes.ByName(configuration.AuthenticationType);
        if (auth.IsEmpty)
            return GenericResult<IGenericConnection>.Failure(
                MsSqlConnectionFactoryLogger.AuthenticationTypeUnknown(
                    _logger, name, configuration.AuthenticationType!));

        MsSqlConnectionFactoryLogger.TraceAuthTypeResolved(_logger, configuration.AuthenticationType!, name);
        MsSqlConnectionFactoryLogger.CreatingConnection(_logger, name);

        // Why normalize: TypeOption lookups are OrdinalIgnoreCase, and seed data drifts in case.
        var normalizedValues = NormalizeAuthValues(configuration.AdditionalProperties);

        // NO FALLBACKS: this method never fetches a secret itself -- that is the implementation
        // provider's job, asynchronously, before it calls here. If the authentication type needs one
        // and none was resolved, building the connection string anyway would silently produce a
        // passwordless connection string rather than the one actually configured.
        var requirement = auth.RequiredSecret(normalizedValues);
        if (!requirement.IsSuccess)
            return requirement.ToNewResult<IGenericConnection>();
        if (!requirement.Value.IsEmpty && string.IsNullOrEmpty(resolvedSecret))
            return GenericResult<IGenericConnection>.Failure(
                MsSqlConnectionFactoryLogger.SecretManagerRequired(_logger, name, requirement.Value.KeyName));

        var built = BuildConnectionString(configuration, auth, normalizedValues, resolvedSecret, name);
        if (!built.IsSuccess)
            return built.ToNewResult<IGenericConnection>();

        return GenericResult<IGenericConnection>.Success(new MsSqlConnection(
            _connectionLogger,
            configuration,
            built.Value.ConnectionString,
            _authenticationContextAccessor));
    }



    /// <summary>
    /// Result of building a connection string.
    /// </summary>
    private readonly record struct ConnectionStringResult(string ConnectionString);

    private static Dictionary<string, string?> NormalizeAuthValues(IDictionary<string, string?>? source)
    {
        if (source is Dictionary<string, string?> { Comparer: StringComparer ci } d && ReferenceEquals(ci, StringComparer.OrdinalIgnoreCase))
            return d;
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (source is null) return result;
        foreach (var kv in source) result[kv.Key] = kv.Value;
        return result;
    }

    private IGenericResult<ConnectionStringResult> BuildConnectionString(
        MsSqlConnectionConfiguration configuration,
        MsSqlAuthenticationConfiguration auth,
        IReadOnlyDictionary<string, string?> authValues,
        string? resolvedPassword,
        string connectionName)
    {
        if (auth.IsEmpty)
        {
            return GenericResult<ConnectionStringResult>.Failure(
                MsSqlConnectionFactoryLogger.AuthenticationTypeNotSpecified(_logger, connectionName));
        }

        var validation = auth.Validate(authValues);
        if (!validation.IsSuccess)
        {
            return validation.ToNewResult<ConnectionStringResult>();
        }

        var fragmentResult = auth.BuildAuthFragment(authValues, resolvedPassword);
        if (!fragmentResult.IsSuccess || fragmentResult.Value is null)
        {
            return fragmentResult.ToNewResult<ConnectionStringResult>();
        }

        var connectionString = BuildConnectionStringBuilder(configuration, fragmentResult.Value).ConnectionString;
        return GenericResult<ConnectionStringResult>.Success(new ConnectionStringResult(connectionString));
    }

    /// <summary>
    /// Builds the full connection string as named <see cref="SqlConnectionStringBuilder"/> properties.
    /// </summary>
    /// <remarks>
    /// The auth fragment (e.g. "User Id=x;Password=y;") is parsed into the builder via its
    /// constructor — SqlConnectionStringBuilder recognizes those keys as strongly-typed properties
    /// (UserID/Password) — then every other option is set by name. No raw key=value string appends.
    /// </remarks>
    private static SqlConnectionStringBuilder BuildConnectionStringBuilder(MsSqlConnectionConfiguration configuration, string authFragment)
    {
        var builder = new SqlConnectionStringBuilder(authFragment)
        {
            DataSource = BuildServerPart(configuration),
            ConnectTimeout = configuration.ConnectionTimeoutSeconds,
            Encrypt = configuration.Encrypt,
            TrustServerCertificate = configuration.TrustServerCertificate,
            Pooling = configuration.EnableConnectionPooling,
            MinPoolSize = configuration.MinPoolSize,
            MaxPoolSize = configuration.MaxPoolSize,
            MultipleActiveResultSets = configuration.EnableMultipleActiveResultSets,
        };

        if (!string.IsNullOrEmpty(configuration.Database))
            builder.InitialCatalog = configuration.Database;

        if (!string.IsNullOrEmpty(configuration.ApplicationName))
            builder.ApplicationName = configuration.ApplicationName;

        return builder;
    }

    // Why: named instances and non-default ports are both expressed as part of the DataSource value
    // itself (SqlConnectionStringBuilder has no separate Port/InstanceName property).
    private static string BuildServerPart(MsSqlConnectionConfiguration configuration)
    {
        if (!string.IsNullOrEmpty(configuration.InstanceName))
            return $"{configuration.Server}\\{configuration.InstanceName}";
        if (configuration.Port != 1433 && configuration.Port > 0)
            return $"{configuration.Server},{configuration.Port}";
        return configuration.Server;
    }

}
