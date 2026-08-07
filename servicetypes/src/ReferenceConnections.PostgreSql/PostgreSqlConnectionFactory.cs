using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.PostgreSql.Authentication;
using Fdw.Services.Connections.PostgreSql.Logging;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Microsoft.Extensions.Logging;
using Npgsql;

using Fdw.Services.Connections.PostgreSql;

using Fdw.Services.Connections.PostgreSql.Discovery;

using Fdw.Services.Connections.PostgreSql.Results;

using Fdw.Services.Connections.PostgreSql.Commands;

using Fdw.Services.Connections.PostgreSql.Validation;


namespace ReferenceConnections.PostgreSql;

/// <summary>
/// Factory for creating PostgreSQL connection instances.
/// </summary>
public sealed class PostgreSqlConnectionFactory : IPostgreSqlConnectionFactory
{
    private readonly ILogger<PostgreSqlConnectionFactory> _logger;
    private readonly ILogger<PostgreSqlConnection> _connectionLogger;
    // Why: the factory owns secret resolution, exactly as HttpConnectionFactory owns its
    // IHttpClientFactory — PostgreSqlConnectionType registers what this factory needs. Null only on the
    // provider-less constructor below, and the authentication type decides whether that matters.
    private readonly ISecretManagerProvider? _secretManagerProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlConnectionFactory"/> class that resolves
    /// secrets through the supplied secret-manager provider.
    /// </summary>
    /// <param name="logger">The logger for factory operations.</param>
    /// <param name="connectionLogger">The logger for connection instances.</param>
    /// <param name="secretManagerProvider">The secret-manager provider, resolved by name per connection.</param>
    public PostgreSqlConnectionFactory(
        ILogger<PostgreSqlConnectionFactory> logger,
        ILogger<PostgreSqlConnection> connectionLogger,
        ISecretManagerProvider secretManagerProvider)
    {
        _logger = logger;
        _connectionLogger = connectionLogger;
        _secretManagerProvider = secretManagerProvider;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlConnectionFactory"/> class with no
    /// secret-manager provider — usable only for connections whose authentication needs no secret.
    /// </summary>
    /// <param name="logger">The logger for factory operations.</param>
    /// <param name="connectionLogger">The logger for connection instances.</param>
    public PostgreSqlConnectionFactory(
        ILogger<PostgreSqlConnectionFactory> logger,
        ILogger<PostgreSqlConnection> connectionLogger)
    {
        _logger = logger;
        _connectionLogger = connectionLogger;
        _secretManagerProvider = null;
    }

    /// <summary>
    /// Creates a PostgreSQL connection using the generic configuration interface (sync overload).
    /// </summary>
    /// <param name="configuration">The connection configuration.</param>
    public IGenericResult<IGenericConnection> Create(IGenericConfiguration configuration)
    {
        PostgreSqlConnectionLog.TraceFactoryCreateGenericEntry(_logger, configuration?.GetType().Name ?? "null");

        if (configuration == null)
        {
            return GenericResult<IGenericConnection>.Failure(
                PostgreSqlConnectionLog.ConfigurationNull(_logger));
        }

        // Why: After config-split DefaultConnectionProvider passes composed ConnectionConfiguration header.
        if (configuration is ConnectionConfiguration header
            && header.Configuration is PostgreSqlConnectionConfiguration typedBody)
        {
            return CreateInternal(typedBody, header.Name);
        }

        if (configuration is PostgreSqlConnectionConfiguration pgConfig)
        {
            return CreateInternal(pgConfig, string.Empty);
        }

        return GenericResult<IGenericConnection>.Failure(
            PostgreSqlConnectionLog.InvalidConfigurationType(_logger, configuration.GetType().Name));
    }

    /// <inheritdoc />
    // Why: async path used by ConfigurationGateway during its connection bootstrap, where the caller
    // already holds a specific secret manager. Awaits ISecretManager.Execute directly instead of going
    // through the secret-manager service provider's config lookup. Falls back to the sync Create when
    // no manager is supplied — which fails loud on its own if this connection's auth needs one.
    public async Task<IGenericResult<IGenericConnection>> Create(
        IGenericConfiguration configuration,
        ISecretManager? secretManager,
        CancellationToken cancellationToken = default)
    {
        if (configuration is null)
        {
            return GenericResult<IGenericConnection>.Failure(
                PostgreSqlConnectionLog.ConfigurationNull(_logger));
        }

        // Why: After config-split DefaultConnectionProvider passes a composed ConnectionConfiguration
        // header. Extract connectionName and typed body from the header.
        if (configuration is ConnectionConfiguration header
            && header.Configuration is PostgreSqlConnectionConfiguration typedBodyFromHeader)
        {
            return secretManager is null
                ? CreateInternal(typedBodyFromHeader, header.Name)
                : await CreateAsyncInternal(typedBodyFromHeader, header.Name, secretManager, cancellationToken).ConfigureAwait(false);
        }

        if (configuration is not PostgreSqlConnectionConfiguration pgConfig)
        {
            return GenericResult<IGenericConnection>.Failure(
                PostgreSqlConnectionLog.InvalidConfigurationType(_logger, configuration.GetType().Name));
        }

        return secretManager is null
            ? CreateInternal(pgConfig, string.Empty)
            : await CreateAsyncInternal(pgConfig, string.Empty, secretManager, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    // Why: this factory reads the "SecretManagerName" key out of the typed body's AdditionalProperties
    // dict and awaits resolution via the secret-manager provider it was constructed with.
    public async Task<IGenericResult<IGenericConnection>> Create(
        IGenericConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        if (configuration is null)
            return GenericResult<IGenericConnection>.Failure(PostgreSqlConnectionLog.ConfigurationNull(_logger));

        var (typedBody, name) = configuration switch
        {
            ConnectionConfiguration header when header.Configuration is PostgreSqlConnectionConfiguration body => (body, header.Name),
            PostgreSqlConnectionConfiguration flat => (flat, string.Empty),
            _ => (null, string.Empty),
        };
        if (typedBody is null)
            return GenericResult<IGenericConnection>.Failure(
                PostgreSqlConnectionLog.InvalidConfigurationType(_logger, configuration.GetType().Name));

        var effectiveName = name.Length > 0 ? name : typedBody.ConnectionId.ToString();

        var authTypeName = typedBody.AuthenticationType;
        if (string.IsNullOrEmpty(authTypeName))
            return GenericResult<IGenericConnection>.Failure(
                PostgreSqlConnectionLog.AuthenticationTypeNotSpecified(_logger, effectiveName));

        // Why: no SecretKeyName => no password injection required; the sync pure-construction path
        // builds it directly (it only fails loud when a secret IS required).
        var secretKeyName = GetAuthValue(typedBody, "SecretKeyName");
        if (string.IsNullOrEmpty(secretKeyName))
            return CreateInternal(typedBody, name);

        // Why: the manager NAME comes from the auth configuration in the typed body — never a
        // hardcoded default guess. Resolve it through the FDW provider and await; a miss fails loud.
        var secretManagerName = GetAuthValue(typedBody, "SecretManagerName");
        if (string.IsNullOrEmpty(secretManagerName))
            return GenericResult<IGenericConnection>.Failure(
                PostgreSqlConnectionLog.SecretManagerProviderNotAvailable(_logger, effectiveName, secretKeyName));

        if (_secretManagerProvider is null)
            return GenericResult<IGenericConnection>.Failure(
                PostgreSqlConnectionLog.SecretManagerProviderNotAvailable(_logger, effectiveName, secretKeyName));

        var managerResult = await _secretManagerProvider.Get(secretManagerName, cancellationToken).ConfigureAwait(false);
        if (!managerResult.IsSuccess || managerResult.Value is null)
            return GenericResult<IGenericConnection>.Failure(
                PostgreSqlConnectionLog.SecretManagerNotFound(_logger, effectiveName, secretManagerName));

        return await CreateAsyncInternal(typedBody, name, managerResult.Value, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a PostgreSQL connection using the generic configuration interface with connection type specification.
    /// </summary>
    public IGenericResult<IGenericConnection> Create(IGenericConfiguration configuration, string connectionType)
    {
        PostgreSqlConnectionLog.TraceFactoryCreateWithTypeEntry(_logger, connectionType);

        if (!string.Equals(connectionType, "PostgreSql", StringComparison.OrdinalIgnoreCase))
        {
            return GenericResult<IGenericConnection>.Failure(
                PostgreSqlConnectionLog.UnsupportedConnectionType(_logger, connectionType));
        }

        return Create(configuration);
    }

    /// <summary>
    /// Creates a PostgreSQL connection using the typed configuration.
    /// </summary>
    public IGenericResult<IGenericConnection> Create(PostgreSqlConnectionConfiguration configuration)
        // Why: Direct typed-body path has no ConnectionConfiguration header — name not available.
        => CreateInternal(configuration, string.Empty);

    private IGenericResult<IGenericConnection> CreateInternal(
        PostgreSqlConnectionConfiguration configuration, string connectionName)
    {
        PostgreSqlConnectionLog.TraceFactoryCreateEntry(_logger, connectionName.Length > 0 ? connectionName : configuration?.ConnectionId.ToString() ?? "null");

        if (configuration == null)
        {
            return GenericResult<IGenericConnection>.Failure(
                PostgreSqlConnectionLog.ConfigurationNull(_logger));
        }

        try
        {
            PostgreSqlConnectionLog.BuildingConnectionString(_logger, connectionName);

            var authTypeName = configuration.AuthenticationType;
            if (string.IsNullOrEmpty(authTypeName))
            {
                return GenericResult<IGenericConnection>.Failure(
                    PostgreSqlConnectionLog.AuthenticationTypeNotSpecified(_logger, connectionName));
            }

            var authValues = NormalizeAuthValues(configuration.AdditionalProperties);
            var auth = PostgreSqlAuthenticationTypes.ByName(authTypeName);

            // Why: the SYNC path never resolves a secret — secret resolution is async. If this
            // connection's auth requires one, FAIL LOUD — the caller must build it through the
            // connection provider (which supplies the FDW secret-manager provider) via the async
            // Create(config, secretManagerProvider, ct) overload. No-secret auth builds fine here.
            authValues.TryGetValue("SecretKeyName", out var secretKeyName);
            if (!string.IsNullOrEmpty(secretKeyName))
            {
                return GenericResult<IGenericConnection>.Failure(
                    PostgreSqlConnectionLog.SecretManagerProviderNotAvailable(_logger, connectionName, secretKeyName));
            }

            var fragmentResult = auth.BuildAuthFragment(authValues, resolvedPassword: null);
            if (!fragmentResult.IsSuccess || fragmentResult.Value is null)
            {
                return fragmentResult.ToNewResult<IGenericConnection>();
            }

            var connectionString = BuildConnectionStringBuilder(configuration, fragmentResult.Value).ConnectionString;

            // Create NpgsqlConnection - connection string goes out of scope after this
            var npgsqlConnection = new NpgsqlConnection(connectionString);

            var connection = new PostgreSqlConnection(_connectionLogger, configuration, npgsqlConnection, connectionName);

            PostgreSqlConnectionLog.ConnectionCreated(_logger, connectionName, configuration.Host, configuration.Port, configuration.Database);

            return GenericResult<IGenericConnection>.Success(connection);
        }
        catch (NpgsqlException ex)
        {
            return GenericResult<IGenericConnection>.Failure(
                PostgreSqlConnectionLog.CreationFailed(_logger, ex, connectionName));
        }
        catch (InvalidOperationException ex)
        {
            return GenericResult<IGenericConnection>.Failure(
                PostgreSqlConnectionLog.CreationFailed(_logger, ex, connectionName));
        }
        catch (ArgumentException ex)
        {
            return GenericResult<IGenericConnection>.Failure(
                PostgreSqlConnectionLog.CreationFailed(_logger, ex, connectionName));
        }
    }

    // Why: the async counterpart of CreateInternal — the only place in this factory that awaits
    // secret resolution. Shared by both async Create overloads (pre-resolved secretManager from the
    // bootstrap path, and provider-resolved secretManager from the runtime path).
    private async Task<IGenericResult<IGenericConnection>> CreateAsyncInternal(
        PostgreSqlConnectionConfiguration configuration,
        string connectionName,
        ISecretManager secretManager,
        CancellationToken cancellationToken)
    {
        var effectiveName = connectionName.Length > 0 ? connectionName : configuration.ConnectionId.ToString();
        try
        {
            PostgreSqlConnectionLog.BuildingConnectionString(_logger, effectiveName);

            var authTypeName = configuration.AuthenticationType;
            if (string.IsNullOrEmpty(authTypeName))
            {
                return GenericResult<IGenericConnection>.Failure(
                    PostgreSqlConnectionLog.AuthenticationTypeNotSpecified(_logger, effectiveName));
            }

            var authValues = NormalizeAuthValues(configuration.AdditionalProperties);
            var auth = PostgreSqlAuthenticationTypes.ByName(authTypeName);

            // Why: hand the dict + secret manager to the auth TypeOption — it owns its key set
            // (Username, SecretKeyName, etc.), validation, and secret resolution. The factory
            // never reaches into AdditionalProperties for auth-specific keys.
            var fragmentResult = await auth.BuildAuthFragment(authValues, secretManager, cancellationToken).ConfigureAwait(false);
            if (!fragmentResult.IsSuccess || fragmentResult.Value is null)
            {
                return fragmentResult.ToNewResult<IGenericConnection>();
            }

            var connectionString = BuildConnectionStringBuilder(configuration, fragmentResult.Value).ConnectionString;
            var npgsqlConnection = new NpgsqlConnection(connectionString);
            var connection = new PostgreSqlConnection(_connectionLogger, configuration, npgsqlConnection, effectiveName);

            PostgreSqlConnectionLog.ConnectionCreated(_logger, effectiveName, configuration.Host, configuration.Port, configuration.Database);

            return GenericResult<IGenericConnection>.Success(connection);
        }
        catch (NpgsqlException ex)
        {
            return GenericResult<IGenericConnection>.Failure(
                PostgreSqlConnectionLog.CreationFailed(_logger, ex, effectiveName));
        }
        catch (InvalidOperationException ex)
        {
            return GenericResult<IGenericConnection>.Failure(
                PostgreSqlConnectionLog.CreationFailed(_logger, ex, effectiveName));
        }
        catch (ArgumentException ex)
        {
            return GenericResult<IGenericConnection>.Failure(
                PostgreSqlConnectionLog.CreationFailed(_logger, ex, effectiveName));
        }
    }

    /// <summary>
    /// Builds the full connection string as named <see cref="NpgsqlConnectionStringBuilder"/> properties.
    /// </summary>
    /// <remarks>
    /// The auth fragment (e.g. "Username=x;Password=y;") is parsed into the builder via its
    /// constructor — NpgsqlConnectionStringBuilder recognizes those keys as strongly-typed
    /// properties — then every other option is set by name. No raw key=value string appends.
    /// </remarks>
    private static NpgsqlConnectionStringBuilder BuildConnectionStringBuilder(PostgreSqlConnectionConfiguration configuration, string authFragment)
    {
        var builder = new NpgsqlConnectionStringBuilder(authFragment)
        {
            Host = configuration.Host,
            Port = configuration.Port,
            Timeout = configuration.ConnectionTimeout,
            CommandTimeout = configuration.CommandTimeout,
            MinPoolSize = configuration.MinPoolSize,
            MaxPoolSize = configuration.MaxPoolSize,
        };

        if (!string.IsNullOrEmpty(configuration.Database))
            builder.Database = configuration.Database;

        if (!string.IsNullOrEmpty(configuration.ApplicationName))
            builder.ApplicationName = configuration.ApplicationName;

        if (!string.Equals(configuration.DefaultSchema, "public", StringComparison.Ordinal))
            builder.SearchPath = configuration.DefaultSchema;

        // Why: SslMode is a free-form string column (Disable/Allow/Prefer/Require/VerifyCA/VerifyFull)
        // but a strongly-typed enum on the builder — Enum.Parse throws (caught by the caller as
        // ArgumentException) rather than silently ignoring an invalid configured value.
        if (!string.IsNullOrEmpty(configuration.SslMode))
            builder.SslMode = Enum.Parse<SslMode>(configuration.SslMode, ignoreCase: true);

        return builder;
    }

    private static string? GetAuthValue(PostgreSqlConnectionConfiguration config, string key)
    {
        if (config.AdditionalProperties is null) return null;
        return config.AdditionalProperties.TryGetValue(key, out var value) ? value : null;
    }

    private static Dictionary<string, string?> NormalizeAuthValues(IDictionary<string, string?>? source)
    {
        if (source is Dictionary<string, string?> { Comparer: StringComparer ci } d && ReferenceEquals(ci, StringComparer.OrdinalIgnoreCase))
            return d;
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (source is null) return result;
        foreach (var kv in source) result[kv.Key] = kv.Value;
        return result;
    }

    #region IServiceFactory Implementation

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
            PostgreSqlConnectionLog.UnsupportedConnectionType(_logger, typeof(T).Name));
    }

    IGenericResult<IGenericService> IServiceFactory.Create(IGenericConfiguration configuration)
    {
        var result = Create(configuration);
        if (!result.IsSuccess || result.Value == null)
        {
            return result.ToNewResult<IGenericService>();
        }

        return GenericResult<IGenericService>.Success(result.Value);
    }

    IGenericResult<IGenericConnection> IServiceFactory<IGenericConnection>.Create(IGenericConfiguration configuration)
    {
        return Create(configuration);
    }

    IGenericResult<IGenericConnection> IServiceFactory<IGenericConnection, PostgreSqlConnectionConfiguration>.Create(
        PostgreSqlConnectionConfiguration configuration)
    {
        return Create(configuration);
    }

    #endregion
}
