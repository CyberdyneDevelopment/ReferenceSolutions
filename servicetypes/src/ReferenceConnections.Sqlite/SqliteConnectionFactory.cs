using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Data.Sqlite.Logging;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Fdw.Services.Connections.Sqlite;

using Fdw.Services.Connections.Sqlite.Authentication;

using Fdw.Services.Connections.Sqlite.Commands;

using Fdw.Services.Connections.Sqlite.Validation;

namespace ReferenceConnections.Sqlite;

/// <summary>
/// Factory for creating SQLite connection instances.
/// </summary>
public sealed class SqliteConnectionFactory : ISqliteConnectionFactory
{
    private readonly ILogger<SqliteConnectionFactory> _logger;
    private readonly ILogger<SqliteConnection> _connectionLogger;
    // Why: the factory owns secret resolution, exactly as HttpConnectionFactory owns its
    // IHttpClientFactory — SqliteConnectionType registers what this factory needs. Null only on the
    // provider-less constructor below, and the authentication type decides whether that matters.
    private readonly ISecretManagerProvider? _secretManagerProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteConnectionFactory"/> class that resolves
    /// secrets through the supplied secret-manager provider.
    /// </summary>
    /// <param name="logger">The logger for factory operations.</param>
    /// <param name="connectionLogger">The logger for connection instances.</param>
    /// <param name="secretManagerProvider">The secret-manager provider, resolved by name per connection.</param>
    public SqliteConnectionFactory(
        ILogger<SqliteConnectionFactory> logger,
        ILogger<SqliteConnection> connectionLogger,
        ISecretManagerProvider secretManagerProvider)
    {
        _logger = logger ?? NullLogger<SqliteConnectionFactory>.Instance;
        _connectionLogger = connectionLogger ?? NullLogger<SqliteConnection>.Instance;
        _secretManagerProvider = secretManagerProvider;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteConnectionFactory"/> class with no
    /// secret-manager provider — usable only for connections whose authentication needs no secret.
    /// </summary>
    /// <param name="logger">The logger for factory operations.</param>
    /// <param name="connectionLogger">The logger for connection instances.</param>
    public SqliteConnectionFactory(
        ILogger<SqliteConnectionFactory> logger,
        ILogger<SqliteConnection> connectionLogger)
    {
        _logger = logger ?? NullLogger<SqliteConnectionFactory>.Instance;
        _connectionLogger = connectionLogger ?? NullLogger<SqliteConnection>.Instance;
        _secretManagerProvider = null;
    }

    /// <inheritdoc/>
    // Why: sync path builds only NoAuth (None) connections — an encryption-key connection needs async
    // secret resolution. AuthMethodNeedsSecret fails loud rather than silently building an unencrypted
    // connection string. Mirrors MsSql/PostgreSql sync-fail-loud.
    public IGenericResult<IGenericConnection> Create(IGenericConfiguration configuration)
    {
        SqliteConnectionLog.TraceFactoryCreateEntry(_logger, configuration?.GetType().Name ?? "null");

        var (typed, name) = Unwrap(configuration);
        if (typed is null)
            return GenericResult<IGenericConnection>.Failure(
                configuration is null
                    ? SqliteConnectionLog.ConfigurationNull(_logger)
                    : SqliteConnectionLog.InvalidConfigurationType(_logger, configuration.GetType().Name));

        if (AuthMethodNeedsSecret(typed))
            return GenericResult<IGenericConnection>.Failure(
                SqliteConnectionLog.SecretManagerNotFound(_logger, EffectiveName(typed, name), typed.AuthenticationType ?? "(none)"));

        return Build(typed, name, password: null);
    }

    /// <inheritdoc/>
    // Why: the bootstrap path holds an already-resolved secret manager, but SQLite is NEVER the
    // ConfigurationDb bootstrap connection — an encryption-key SQLite here has no provider to resolve its
    // named manager, so fail loud rather than silently build an unencrypted string. A None connection builds.
    public Task<IGenericResult<IGenericConnection>> Create(
        IGenericConfiguration configuration,
        ISecretManager? secretManager,
        CancellationToken cancellationToken = default)
    {
        var (typed, name) = Unwrap(configuration);
        if (typed is null)
            return Task.FromResult(GenericResult<IGenericConnection>.Failure(
                configuration is null
                    ? SqliteConnectionLog.ConfigurationNull(_logger)
                    : SqliteConnectionLog.InvalidConfigurationType(_logger, configuration.GetType().Name)));

        if (AuthMethodNeedsSecret(typed))
            return Task.FromResult(GenericResult<IGenericConnection>.Failure(
                SqliteConnectionLog.SecretManagerNotFound(_logger, EffectiveName(typed, name), typed.AuthenticationType ?? "(none)")));

        return Task.FromResult(Build(typed, name, password: null));
    }

    /// <inheritdoc/>
    // Why: the connection's authentication method (SqliteAuthenticationTypes) parses its own KVP and
    // resolves the named manager through the provider this factory was constructed with.
    public async Task<IGenericResult<IGenericConnection>> Create(
        IGenericConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var (typed, name) = Unwrap(configuration);
        if (typed is null)
            return GenericResult<IGenericConnection>.Failure(
                configuration is null
                    ? SqliteConnectionLog.ConfigurationNull(_logger)
                    : SqliteConnectionLog.InvalidConfigurationType(_logger, configuration.GetType().Name));

        return await CreateWithAuth(typed, name, _secretManagerProvider, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public IGenericResult<IGenericConnection> Create(SqliteConnectionConfiguration configuration)
        => Create((IGenericConfiguration)configuration);

    private static (SqliteConnectionConfiguration? Typed, string Name) Unwrap(IGenericConfiguration? configuration)
        => configuration switch
        {
            ConnectionConfiguration header when header.Configuration is SqliteConnectionConfiguration body => (body, header.Name),
            SqliteConnectionConfiguration flat => (flat, string.Empty),
            _ => (null, string.Empty),
        };

    private static string EffectiveName(SqliteConnectionConfiguration configuration, string connectionName)
        => connectionName.Length > 0 ? connectionName : configuration.ConnectionId.ToString();

    // Why: the auth method decides whether a secret is needed — None does not, EncryptionKey does.
    private static bool AuthMethodNeedsSecret(SqliteConnectionConfiguration configuration)
        => !string.IsNullOrEmpty(configuration.AuthenticationType)
           && !string.Equals(configuration.AuthenticationType, "None", StringComparison.OrdinalIgnoreCase);

    private async Task<IGenericResult<IGenericConnection>> CreateWithAuth(
        SqliteConnectionConfiguration configuration,
        string connectionName,
        ISecretManagerProvider? secretManagerProvider,
        CancellationToken cancellationToken)
    {
        var effectiveName = EffectiveName(configuration, connectionName);

        if (string.IsNullOrWhiteSpace(configuration.DataSource))
            return GenericResult<IGenericConnection>.Failure(
                SqliteConnectionLog.DataSourceMissing(_logger, effectiveName));

        // Why: route secret resolution through the connection's authentication method. The auth option
        // (None/EncryptionKey) parses its own KVP and resolves the named manager. No property reads,
        // no hardcoded default.
        var authMethod = SqliteAuthenticationTypes.ByName(
            string.IsNullOrEmpty(configuration.AuthenticationType) ? "None" : configuration.AuthenticationType);
        if (authMethod.IsEmpty)
            return GenericResult<IGenericConnection>.Failure(
                SqliteConnectionLog.InvalidConfigurationType(_logger, configuration.AuthenticationType ?? "(none)"));

        string? password = null;
        if (AuthMethodNeedsSecret(configuration))
        {
            if (secretManagerProvider is null)
                return GenericResult<IGenericConnection>.Failure(
                    SqliteConnectionLog.SecretManagerNotFound(_logger, effectiveName, configuration.AuthenticationType!));

            // Why: the KVP is exposed as IDictionary; the auth method reads it as a read-only dictionary.
            var authValues = new Dictionary<string, string?>(configuration.AdditionalProperties, StringComparer.OrdinalIgnoreCase);
            var passwordResult = await authMethod
                .ResolvePassword(authValues, secretManagerProvider, cancellationToken)
                .ConfigureAwait(false);
            if (!passwordResult.IsSuccess)
                return passwordResult.ToNewResult<IGenericConnection>();
            password = passwordResult.Value;
        }

        return Build(configuration, connectionName, password);
    }

    private IGenericResult<IGenericConnection> Build(SqliteConnectionConfiguration configuration, string connectionName, string? password)
    {
        var effectiveName = EffectiveName(configuration, connectionName);
        if (string.IsNullOrWhiteSpace(configuration.DataSource))
            return GenericResult<IGenericConnection>.Failure(
                SqliteConnectionLog.DataSourceMissing(_logger, effectiveName));
        try
        {
            var connectionString = BuildConnectionString(configuration, password);
            return GenericResult<IGenericConnection>.Success(
                new SqliteConnection(_connectionLogger, configuration, connectionString));
        }
        catch (Exception ex)
        {
            return GenericResult<IGenericConnection>.Failure(
                SqliteConnectionLog.CreationFailedWithException(_logger, ex, effectiveName));
        }
    }

    private static string BuildConnectionString(SqliteConnectionConfiguration configuration, string? password)
    {
        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $"Data Source={configuration.DataSource}");

        if (!string.Equals(configuration.Mode, "ReadWriteCreate", StringComparison.OrdinalIgnoreCase))
            sb.Append(CultureInfo.InvariantCulture, $";Mode={configuration.Mode}");

        if (!string.Equals(configuration.Cache, "Default", StringComparison.OrdinalIgnoreCase))
            sb.Append(CultureInfo.InvariantCulture, $";Cache={configuration.Cache}");

        if (configuration.ForeignKeys)
            sb.Append(";Foreign Keys=True");

        if (configuration.DefaultTimeout != 30)
            sb.Append(CultureInfo.InvariantCulture, $";Default Timeout={configuration.DefaultTimeout}");

        if (!string.IsNullOrEmpty(password))
            sb.Append(CultureInfo.InvariantCulture, $";Password={password}");

        return sb.ToString();
    }

    #region IServiceFactory explicit implementations

    IGenericResult<T> IServiceFactory.Create<T>(IGenericConfiguration configuration)
    {
        var result = Create(configuration);
        if (!result.IsSuccess || result.Value == null)
            return result.ToNewResult<T>();

        if (result.Value is T typedResult)
            return GenericResult<T>.Success(typedResult);

        return GenericResult<T>.Failure(
            SqliteConnectionLog.InvalidConfigurationType(_logger, typeof(T).Name));
    }

    IGenericResult<IGenericService> IServiceFactory.Create(IGenericConfiguration configuration)
    {
        var result = Create(configuration);
        if (!result.IsSuccess || result.Value == null)
            return result.ToNewResult<IGenericService>();

        return GenericResult<IGenericService>.Success(result.Value);
    }

    IGenericResult<IGenericConnection> IServiceFactory<IGenericConnection>.Create(IGenericConfiguration configuration)
        => Create(configuration);

    IGenericResult<IGenericConnection> IServiceFactory<IGenericConnection, SqliteConnectionConfiguration>.Create(
        SqliteConnectionConfiguration configuration)
        => Create(configuration);

    #endregion
}
