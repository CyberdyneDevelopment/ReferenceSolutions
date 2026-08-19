using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.MsSql.Authentication;
using Fdw.Services.Connections.MsSql.Logging;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;

using Fdw.Services.Connections.MsSql;

using Fdw.Services.Connections.MsSql.Discovery;

using Fdw.Services.Connections.MsSql.Limits;

using Fdw.Services.Connections.MsSql.ErrorHandlers;

using ReferenceConnections.MsSql.Mapping;

using Fdw.Services.Connections.MsSql.Messages;

using Fdw.Services.Connections.MsSql.Results;

using Fdw.Services.Connections.MsSql.Commands;

using Fdw.Services.Connections.MsSql.Validation;



using ReferenceConnections.MsSql.Logging;

namespace ReferenceConnections.MsSql;

/// <summary>
/// Factory for creating Microsoft SQL Server connection instances.
/// </summary>
/// <remarks>
/// This factory accepts <see cref="MsSqlConnectionConfiguration"/> and creates
/// <see cref="MsSqlConnection"/> instances with <see cref="SqlConnection"/> objects.
/// The connection string is built using authentication processors and goes out of scope
/// immediately after creating the SqlConnection for security.
/// </remarks>
public sealed class MsSqlConnectionFactory : IMsSqlConnectionFactory
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
    /// For the connection that reaches ConfigurationDb, which cannot resolve a secret manager by name
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

    // Why: ONE decision, shared by every Create overload, so the rules cannot drift apart.
    // The authentication type — not the factory, and not a hardcoded key name — decides whether a
    // secret is needed at all, and which manager may supply it.
    private async Task<IGenericResult<ISecretManager?>> ResolveSecretManager(
        MsSqlConnectionConfiguration configuration,
        MsSqlAuthenticationConfiguration auth,
        ISecretManager? supplied,
        ISecretManagerProvider? provider,
        string connectionName,
        CancellationToken cancellationToken)
    {
        // Why: an auth type that declares no secret-bearing properties has nothing to resolve, so it
        // simply does not get a secret manager. WindowsAuth, EntraId, ManagedIdentity and AzureCli
        // are in that position; a missing secret manager is not a defect for them.
        if (auth.SecretPropertyNames.Count == 0)
        {
            MsSqlConnectionFactoryLogger.BuiltWithoutSecretManager(_logger, connectionName, auth.Name);
            return GenericResult<ISecretManager?>.Success(null);
        }

        var declared = auth.GetValue(NormalizeAuthValues(configuration.AdditionalProperties), "SecretManagerName");
        if (!declared.IsSuccess)
            return declared.ToNewResult<ISecretManager?>();

        // Why: a supplied manager is the ConfigurationDb path, whose manager is declared in
        // configurationSchema.json rather than resolved by name. It still has to BE the store this
        // connection named — reading a password out of a store the connection never declared is a
        // silent credential substitution, so refuse it.
        if (supplied is not null)
        {
            return string.Equals(supplied.Name, declared.Value, StringComparison.OrdinalIgnoreCase)
                ? GenericResult<ISecretManager?>.Success(supplied)
                : GenericResult<ISecretManager?>.Failure(
                    MsSqlConnectionFactoryLogger.SecretManagerMismatch(_logger, connectionName, declared.Value!, supplied.Name));
        }

        if (provider is null)
        {
            return GenericResult<ISecretManager?>.Failure(
                MsSqlConnectionFactoryLogger.SecretManagerRequiredButNotProvided(_logger, connectionName, auth.Name));
        }

        var resolved = await provider.Get(declared.Value!, cancellationToken).ConfigureAwait(false);
        if (!resolved.IsSuccess || resolved.Value is null)
        {
            return GenericResult<ISecretManager?>.Failure(
                MsSqlConnectionFactoryLogger.SecretManagerNotFound(_logger, connectionName, declared.Value!));
        }

        MsSqlConnectionFactoryLogger.SecretResolvedFromManager(_logger, connectionName, declared.Value!);
        return GenericResult<ISecretManager?>.Success(resolved.Value);
    }

    // Why: the ONE async creation path. Both async overloads differ only in whether the caller already
    // holds a secret manager; everything after that — unwrap, resolve the authentication type, decide
    // whether a secret is needed, build — is identical and lives here.
    private async Task<IGenericResult<IGenericConnection>> CreateCore(
        IGenericConfiguration? configuration,
        ISecretManager? supplied,
        CancellationToken cancellationToken)
    {
        if (configuration is null)
            return GenericResult<IGenericConnection>.Failure(MsSqlConnectionFactoryLogger.ConfigurationNull(_logger));

        var (typedBody, name) = Unwrap(configuration);
        if (typedBody is null)
            return GenericResult<IGenericConnection>.Failure(
                MsSqlConnectionFactoryLogger.InvalidConfigurationType(_logger, configuration.GetType().Name));

        // NO FALLBACKS: a connection with no name cannot be reported, correlated in a log, or
        // referenced by the caller that asked for it. Substituting the id hid that and left readers
        // hunting an identifier that appears in no configuration.
        if (name.Length == 0)
            return GenericResult<IGenericConnection>.Failure(
                MsSqlConnectionFactoryLogger.ConnectionNameMissing(_logger, typedBody.ConnectionId.ToString()));

        if (string.IsNullOrEmpty(typedBody.AuthenticationType))
            return GenericResult<IGenericConnection>.Failure(
                MsSqlConnectionFactoryLogger.AuthenticationTypeNotSpecified(_logger, name));

        // Why: ByName returns the NotFound sentinel (empty Name) for an unrecognized type — never null.
        // An unknown authentication type is a configuration defect, not "no authentication".
        if (MsSqlAuthenticationTypes.ByName(typedBody.AuthenticationType).IsEmpty)
            return GenericResult<IGenericConnection>.Failure(
                MsSqlConnectionFactoryLogger.AuthenticationTypeUnknown(_logger, name, typedBody.AuthenticationType!));

        var manager = await ResolveSecretManager(
            typedBody,
            MsSqlAuthenticationTypes.ByName(typedBody.AuthenticationType),
            supplied,
            _secretManagerProvider,
            name,
            cancellationToken).ConfigureAwait(false);
        if (!manager.IsSuccess)
            return manager.ToNewResult<IGenericConnection>();

        return manager.Value is null
            ? CreateInternal(typedBody, name)
            : await CreateAsyncInternal(typedBody, name, manager.Value, cancellationToken).ConfigureAwait(false);
    }

    private static (MsSqlConnectionConfiguration? TypedBody, string Name) Unwrap(IGenericConfiguration configuration)
        => configuration switch
        {
            ConnectionConfiguration header when header.Configuration is MsSqlConnectionConfiguration body => (body, header.Name),
            MsSqlConnectionConfiguration flat => (flat, string.Empty),
            _ => (null, string.Empty),
        };


    /// <summary>
    /// Creates a SQL Server connection using the generic configuration interface.
    /// </summary>
    /// <param name="configuration">The configuration object (must be MsSqlConnectionConfiguration).</param>
    /// <returns>A result containing the created connection or failure information.</returns>
    public IGenericResult<IGenericConnection> Create(IGenericConfiguration configuration)
    {
        MsSqlConnectionFactoryLogger.TraceCreateGenericEntry(_logger, configuration?.GetType().Name ?? "null");

        if (configuration == null)
        {
            return GenericResult<IGenericConnection>.Failure(
                MsSqlConnectionFactoryLogger.ConfigurationNull(_logger));
        }

        // Why: After config-split DefaultConnectionProvider passes a composed ConnectionConfiguration
        // header. Extract connectionName and typed body from the header.
        var (typedBody, name) = Unwrap(configuration);
        return typedBody is null
            ? GenericResult<IGenericConnection>.Failure(
                MsSqlConnectionFactoryLogger.InvalidConfigurationType(_logger, configuration.GetType().Name))
            : CreateInternal(typedBody, name);
    }

    /// <inheritdoc />
    // Why: the bootstrap path — ConfigurationGateway holds the env-var secret manager declared in
    // configurationSchema.json for the connection that reaches ConfigurationDb, because the provider
    // that would resolve it by name reads its own configuration out of the database being opened.
    public Task<IGenericResult<IGenericConnection>> Create(
        IGenericConfiguration configuration,
        ISecretManager? secretManager,
        CancellationToken cancellationToken = default)
        => CreateCore(configuration, secretManager, cancellationToken);

    /// <inheritdoc />
    public Task<IGenericResult<IGenericConnection>> Create(
        IGenericConfiguration configuration,
        CancellationToken cancellationToken = default)
        => CreateCore(configuration, supplied: null, cancellationToken);

    // Why: CreateCore has already proved the authentication type resolves, so this method only builds.
    private async Task<IGenericResult<IGenericConnection>> CreateAsyncInternal(
        MsSqlConnectionConfiguration msSqlCfg,
        string connectionName,
        ISecretManager secretManager,
        CancellationToken cancellationToken)
    {
        try
        {
            // Why: hand the dict + secret manager to the auth TypeOption — it owns its key set
            // (Username, SecretKeyName, etc.), validation, and secret resolution. The factory
            // never reaches into AdditionalProperties for auth-specific keys.
            var fragmentResult = await MsSqlAuthenticationTypes.ByName(msSqlCfg.AuthenticationType)
                .BuildAuthFragment(NormalizeAuthValues(msSqlCfg.AdditionalProperties), secretManager, cancellationToken)
                .ConfigureAwait(false);
            if (!fragmentResult.IsSuccess || fragmentResult.Value is null)
                return fragmentResult.ToNewResult<IGenericConnection>();

            return GenericResult<IGenericConnection>.Success(new MsSqlConnection(
                _connectionLogger,
                msSqlCfg,
                BuildConnectionStringBuilder(msSqlCfg, fragmentResult.Value).ConnectionString,
                MsSqlAuthenticationTypes.ByName(msSqlCfg.AuthenticationType).UsesAccessToken
                    ? MsSqlAuthenticationTypes.ByName(msSqlCfg.AuthenticationType).AcquireAccessToken()
                    : null,
                _authenticationContextAccessor));
        }
        catch (Exception ex)
        {
            return GenericResult<IGenericConnection>.Failure(
                MsSqlConnectionFactoryLogger.CreationFailed(_logger, connectionName, ex.Message));
        }
    }

    /// <summary>
    /// Creates a SQL Server connection using the generic configuration interface with connection type specification.
    /// </summary>
    /// <param name="configuration">The configuration object.</param>
    /// <param name="connectionType">The connection type (must be "MsSql").</param>
    /// <returns>A result containing the created connection or failure information.</returns>
    public IGenericResult<IGenericConnection> Create(IGenericConfiguration configuration, string connectionType)
    {
        MsSqlConnectionFactoryLogger.TraceCreateWithTypeEntry(_logger, connectionType, configuration.GetType().Name);

        if (!string.Equals(connectionType, "MsSql", StringComparison.OrdinalIgnoreCase))
        {
            return GenericResult<IGenericConnection>.Failure(
                MsSqlConnectionFactoryLogger.UnsupportedConnectionType(_logger, connectionType));
        }

        return Create(configuration);
    }

    /// <summary>
    /// Creates a SQL Server connection using the flat MsSqlConnectionConfiguration.
    /// </summary>
    /// <param name="configuration">The connection configuration.</param>
    /// <returns>A result containing the created connection or failure information.</returns>
    public IGenericResult<IGenericConnection> Create(MsSqlConnectionConfiguration configuration)
        // Why: Direct typed-body path has no ConnectionConfiguration header — name not available.
        => CreateInternal(configuration, string.Empty);

#pragma warning disable MA0051, FDW007
    private IGenericResult<IGenericConnection> CreateInternal(MsSqlConnectionConfiguration configuration, string connectionName)
    {
        var name = connectionName.Length > 0 ? connectionName : configuration?.ConnectionId.ToString() ?? "null";
        MsSqlConnectionFactoryLogger.TraceCreateEntry(_logger, name);

        if (configuration == null)
        {
            return GenericResult<IGenericConnection>.Failure(
                MsSqlConnectionFactoryLogger.ConfigurationNull(_logger));
        }

        try
        {
            MsSqlConnectionFactoryLogger.CreatingConnection(_logger, name);

            // Why: AuthenticationType is a first-class column on MsSqlConnection — never read it from the KVP dict.
            var authTypeName = configuration.AuthenticationType;

            if (string.IsNullOrEmpty(authTypeName))
            {
                return GenericResult<IGenericConnection>.Failure(
                    MsSqlConnectionFactoryLogger.AuthenticationTypeNotSpecified(_logger, name));
            }

            // Why: Normalize to OrdinalIgnoreCase so TypeOption TryGetValue lookups don't miss
            // because of case drift in seed data.
            var authValues = NormalizeAuthValues(configuration.AdditionalProperties);

            var auth = MsSqlAuthenticationTypes.ByName(authTypeName);
            MsSqlConnectionFactoryLogger.TraceAuthTypeResolved(_logger, authTypeName!, name);

            // Why: the SYNC path never resolves a secret — secret resolution is async. The
            // AUTHENTICATION TYPE owns the answer through the properties it declares as
            // secret-bearing; the factory never names a KVP key itself. A no-secret auth
            // (e.g. integrated) builds fine here.
            if (auth.SecretPropertyNames.Count > 0)
            {
                return GenericResult<IGenericConnection>.Failure(
                    MsSqlConnectionFactoryLogger.SecretManagerRequiredButNotProvided(_logger, name, auth.Name));
            }

            // Log resolved configuration
            MsSqlConnectionFactoryLogger.TraceConnectionConfig(
                _logger, name, configuration.Server, configuration.Database,
                configuration.Port, authTypeName!, configuration.Encrypt, configuration.TrustServerCertificate);

            // Build connection string using authentication processor
            MsSqlConnectionFactoryLogger.TraceBuildingConnectionString(_logger, name, authTypeName!);
            var connectionStringResult = BuildConnectionString(configuration, auth, authValues, resolvedPassword: null, name);
            if (!connectionStringResult.IsSuccess)
            {
                return connectionStringResult.ToNewResult<IGenericConnection>();
            }

            // Create connection with connection string — ADO.NET handles pooling
            var connection = new MsSqlConnection(
                _connectionLogger,
                configuration,
                connectionStringResult.Value.ConnectionString,
                connectionStringResult.Value.AccessToken,
                _authenticationContextAccessor);

            MsSqlConnectionFactoryLogger.ConnectionCreated(_logger, name, configuration.Server ?? "unknown");

            return GenericResult<IGenericConnection>.Success(connection);
        }
        catch (Exception ex)
        {
            return GenericResult<IGenericConnection>.Failure(
                MsSqlConnectionFactoryLogger.CreationFailed(_logger, name, ex.Message));
        }
    }
#pragma warning restore MA0051, FDW007

    /// <summary>
    /// Creates a SQL Server connection using the flat configuration with connection type specification.
    /// </summary>
    /// <param name="configuration">The connection configuration.</param>
    /// <param name="connectionType">The connection type (must be "MsSql").</param>
    /// <returns>A result containing the created connection or failure information.</returns>
    public IGenericResult<IGenericConnection> Create(MsSqlConnectionConfiguration configuration, string connectionType)
    {
        MsSqlConnectionFactoryLogger.TraceCreateWithTypeEntry(_logger, connectionType, configuration?.ConnectionId.ToString() ?? "null");

        if (configuration == null)
        {
            return GenericResult<IGenericConnection>.Failure(
                MsSqlConnectionFactoryLogger.ConfigurationNull(_logger));
        }

        if (!string.Equals(connectionType, "MsSql", StringComparison.OrdinalIgnoreCase))
        {
            return GenericResult<IGenericConnection>.Failure(
                MsSqlConnectionFactoryLogger.UnsupportedConnectionType(_logger, connectionType));
        }

        return CreateInternal(configuration, string.Empty);
    }

    /// <summary>
    /// Result of building a connection string, optionally including an access token
    /// for token-based authentication (e.g., AzureCli).
    /// </summary>
    private readonly record struct ConnectionStringResult(string ConnectionString, string? AccessToken);

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

        string? accessToken = null;
        if (auth.UsesAccessToken)
        {
            MsSqlConnectionFactoryLogger.AcquiringAccessToken(_logger, connectionName);
            try
            {
                accessToken = auth.AcquireAccessToken();
                if (string.IsNullOrEmpty(accessToken))
                {
                    return GenericResult<ConnectionStringResult>.Failure(
                        MsSqlConnectionFactoryLogger.AccessTokenAcquisitionFailed(_logger, connectionName, "Token was null or empty"));
                }
                MsSqlConnectionFactoryLogger.AccessTokenAcquired(_logger, connectionName);
            }
            catch (Exception ex)
            {
                return GenericResult<ConnectionStringResult>.Failure(
                    MsSqlConnectionFactoryLogger.AccessTokenAcquisitionFailed(_logger, connectionName, ex.Message));
            }
        }

        var connectionString = BuildConnectionStringBuilder(configuration, fragmentResult.Value).ConnectionString;
        return GenericResult<ConnectionStringResult>.Success(new ConnectionStringResult(connectionString, accessToken));
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

    #region IServiceFactory Implementation

    /// <inheritdoc/>
    IGenericResult<T> IServiceFactory.Create<T>(IGenericConfiguration configuration)
    {
        var result = Create(configuration);
        if (!result.IsSuccess || result.Value == null)
        {
            return result.ToNewResult<T>();
        }

        if (result.Value is T typedResult)
        {
            return GenericResult<T>.Success(typedResult);
        }

        return GenericResult<T>.Failure(
            MsSqlConnectionFactoryLogger.UnexpectedConnectionType(_logger, typeof(T).Name));
    }

    /// <inheritdoc/>
    IGenericResult<IGenericService> IServiceFactory.Create(IGenericConfiguration configuration)
    {
        var result = Create(configuration);
        if (!result.IsSuccess || result.Value == null)
        {
            return result.ToNewResult<IGenericService>();
        }

        return GenericResult<IGenericService>.Success(result.Value);
    }

    /// <inheritdoc/>
    IGenericResult<IGenericConnection> IServiceFactory<IGenericConnection>.Create(IGenericConfiguration configuration)
    {
        return Create(configuration);
    }

    /// <inheritdoc/>
    IGenericResult<IGenericConnection> IServiceFactory<IGenericConnection, MsSqlConnectionConfiguration>.Create(
        MsSqlConnectionConfiguration configuration)
    {
        return Create(configuration);
    }

    #endregion
}
