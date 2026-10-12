using Fdw.Services;
using System;
using System.Globalization;
using System.Text;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Data.Sqlite.Logging;
using Fdw.Services.SecretManagers.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Fdw.Services.Connections.Sqlite;

namespace FdwReferenceConnections.Sqlite;

/// <summary>
/// Factory for creating SQLite connection instances.
/// </summary>
public sealed class SqliteConnectionFactory : ServiceFactoryBase<IGenericConnection, SqliteConnectionConfiguration>, ISqliteConnectionFactory
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
    public override IGenericResult<IGenericConnection> Create(SqliteConnectionConfiguration configuration)
        => Create(configuration, resolvedSecret: null);

    /// <summary>
    /// Creates the connection from the typed configuration and an already-fetched secret.
    /// </summary>
    /// <param name="configuration">The connection configuration.</param>
    /// <param name="resolvedSecret">The encryption key this connection declared, or null for None auth.</param>
    /// <returns>The connection, or a structured failure.</returns>
    /// <remarks>
    /// Synchronous, and every other Create lands here. Fetching the key is the implementation
    /// provider's job; this only builds. An authentication method that needs a key and is handed
    /// none still fails loud — it just fails here rather than by refusing to try.
    /// </remarks>
    public IGenericResult<IGenericConnection> Create(
        SqliteConnectionConfiguration configuration,
        string? resolvedSecret)
    {
        SqliteConnectionLog.TraceFactoryCreateEntry(_logger, configuration?.GetType().Name ?? "null");

        if (configuration is null)
            return GenericResult<IGenericConnection>.Failure(SqliteConnectionLog.ConfigurationNull(_logger));

        if (AuthMethodNeedsSecret(configuration) && string.IsNullOrEmpty(resolvedSecret))
            return GenericResult<IGenericConnection>.Failure(
                SqliteConnectionLog.SecretManagerNotFound(
                    _logger,
                    EffectiveName(configuration, configuration.Name),
                    configuration.AuthenticationType ?? "(none)"));

        return Build(configuration, configuration.Name, resolvedSecret);
    }

    private static (SqliteConnectionConfiguration? Typed, string Name) Unwrap(IGenericConfiguration? configuration)
        => configuration switch
        {
            SqliteConnectionConfiguration typed => (typed, typed.Name),
            _ => (null, string.Empty),
        };

    private static string EffectiveName(SqliteConnectionConfiguration configuration, string connectionName)
        => connectionName.Length > 0 ? connectionName : configuration.Id.ToString();

    // Why: the auth method decides whether a secret is needed — None does not, EncryptionKey does.
    private static bool AuthMethodNeedsSecret(SqliteConnectionConfiguration configuration)
        => !string.IsNullOrEmpty(configuration.AuthenticationType)
           && !string.Equals(configuration.AuthenticationType, "None", StringComparison.OrdinalIgnoreCase);


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

}
