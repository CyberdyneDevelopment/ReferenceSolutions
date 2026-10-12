using Fdw.Services;
using System;
using Fdw.Commands.Data.Abstractions.Logging;
using System.Linq;
using System.Collections.Generic;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.PostgreSql.Authentication;
using Fdw.Services.Connections.PostgreSql.Logging;
using Fdw.Services.SecretManagers.Abstractions;
using Microsoft.Extensions.Logging;
using Npgsql;

using Fdw.Services.Connections.PostgreSql;


namespace FdwReferenceConnections.PostgreSql;

/// <summary>
/// Factory for creating PostgreSQL connection instances.
/// </summary>
public sealed class PostgreSqlConnectionFactory : ServiceFactoryBase<IGenericConnection, PostgreSqlConnectionConfiguration>, IPostgreSqlConnectionFactory
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
    /// Creates a PostgreSQL connection using the typed configuration.
    /// </summary>
    public override IGenericResult<IGenericConnection> Create(PostgreSqlConnectionConfiguration configuration)
        => Create(configuration, resolvedSecret: null);

    /// <summary>
    /// Creates a PostgreSQL connection from the typed configuration and an already-fetched secret.
    /// </summary>
    /// <param name="configuration">The connection configuration.</param>
    /// <param name="resolvedSecret">The value read for this connection's declared secret, or null.</param>
    /// <returns>The connection, or a structured failure.</returns>
    /// <remarks>
    /// Synchronous, and every other Create lands here. Fetching the secret is the implementation
    /// provider's job; this only builds, and building never needs to await. The authentication type
    /// still produces the secret portion of the connection string from the value it is handed.
    /// </remarks>
    public IGenericResult<IGenericConnection> Create(
        PostgreSqlConnectionConfiguration configuration,
        string? resolvedSecret)
    {
        var connectionName = configuration?.Name ?? string.Empty;
        PostgreSqlConnectionLog.TraceFactoryCreateEntry(_logger, connectionName.Length > 0 ? connectionName : configuration?.Id.ToString() ?? "null");

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

            // Never defaulted (FDW-1152): how many rows an insert runs serially before it takes the bulk route is stated in the
            // connection's configuration, and a connection that does not state it is refused, naming it.
            if (configuration.MaxSerialInsert <= 0)
            {
                return GenericResult<IGenericConnection>.Failure(DatabaseCommandLog.MaxSerialInsertNotStated(_logger, connectionName));
            }

            var authValues = NormalizeAuthValues(configuration.AdditionalProperties);
            var auth = PostgreSqlAuthenticationTypes.ByName(authTypeName);

            // The authentication type builds the secret portion from the value the provider fetched.
            // A type that declares no secret ignores it; one that does gets what it asked for.
            var fragmentResult = auth.BuildAuthFragment(authValues, resolvedSecret);
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
                ExceptionResultExtensions.FlattenException(ex).Prepend(PostgreSqlConnectionLog.CreationFailed(_logger, ex, connectionName)));
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

}
