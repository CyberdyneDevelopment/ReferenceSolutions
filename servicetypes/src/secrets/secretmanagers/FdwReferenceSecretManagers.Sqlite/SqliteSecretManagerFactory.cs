using System;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.Settings;
using FdwReferenceSecretManagers.Sqlite.Configuration;
using FdwReferenceSecretManagers.Sqlite.Logging;
using FdwReferenceSecretManagers.Sqlite.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services;
using Fdw.Conventions;

namespace FdwReferenceSecretManagers.Sqlite;

/// <summary>
/// Factory for creating <see cref="SqliteSecretManager"/> instances.
/// </summary>
/// <remarks>
/// Opens a transient connection to verify the data source is reachable and ensures
/// the secrets table exists before returning the manager. The manager itself opens
/// per-operation connections via <c>SqliteExecutionContext.CreateConnection()</c>.
/// </remarks>
public sealed class SqliteSecretManagerFactory : ServiceFactoryBase<ISecretManager, SqliteSecretManagerConfiguration>, ISqliteSecretManagerFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<SqliteSecretManagerFactory> _logger;
    // Why Lazy: the settings are read through connections, so a factory that took the provider itself would wait on the connection it is
    // building (FDW-3284). Null in a host that registers no settings.
    private readonly Lazy<IEffectiveSettingsProvider>? _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteSecretManagerFactory"/> class.
    /// </summary>
    /// <param name="loggerFactory">The logger factory for creating service loggers.</param>
    /// <param name="settings">The host's settings, read for the command timeout of a secret manager that states none (FDW-3284). Optional: a host that registers no settings never builds one without a stated timeout.</param>
    public SqliteSecretManagerFactory(ILoggerFactory? loggerFactory, Lazy<IEffectiveSettingsProvider>? settings = null)
    {
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger<SqliteSecretManagerFactory>();
        _settings = settings;
    }

    /// <inheritdoc/>
    public Task<IGenericResult<ISecretManager>> CreateSecretManager(SqliteSecretManagerConfiguration configuration)
        => CreateSecretManagerInternal(configuration, configuration.Name);

    private async Task<IGenericResult<ISecretManager>> CreateSecretManagerInternal(
        SqliteSecretManagerConfiguration configuration, string secretManagerName)
    {
        SqliteSecretManagerLogger.TraceCreateSecretManagerEntry(_logger);

        var effectiveName = secretManagerName.Length > 0 ? secretManagerName : configuration.Id.ToString();

        try
        {
            SqliteSecretManagerLogger.CreatingSecretManager(_logger, effectiveName);

            // Never defaulted (FDW-3284): the command timeout is the secret manager's own, else the instance's setting
            // DefaultCommandTimeoutSeconds, and a secret manager that has neither is refused, naming it and the setting.
            var commandTimeout = InstanceCommandTimeout.Resolve(configuration.CommandTimeoutSeconds, _settings, "secret manager", effectiveName, _logger);
            if (commandTimeout.IsFailure)
            {
                return commandTimeout.ToNewResult<ISecretManager>();
            }

            var ensureResult = await EnsureTableExists(configuration, commandTimeout.Value).ConfigureAwait(false);
            if (ensureResult.IsFailure)
            {
                return ensureResult.ToNewResult<ISecretManager>();
            }

            var serviceLogger = _loggerFactory.CreateLogger<SqliteSecretManager>();
            var service = new SqliteSecretManager(serviceLogger, configuration, commandTimeout.Value, effectiveName);

            SqliteSecretManagerLogger.SecretManagerCreated(_logger, effectiveName);
            return GenericResult<ISecretManager>.Success(service);
        }
        catch (Exception ex)
        {
            return GenericResult<ISecretManager>.Failure(
                SqliteSecretManagerLogger.CreationFailed(_logger, ex, ex.Message));
        }
    }

    private async Task<IGenericResult> EnsureTableExists(SqliteSecretManagerConfiguration configuration, int commandTimeoutSeconds)
    {
        SqliteSecretManagerLogger.EnsuringTable(_logger, configuration.TableName, configuration.DataSource);

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = configuration.DataSource
        }.ToString();

        var conn = new SqliteConnection(connectionString);
        await using (conn.ConfigureAwait(false))
        {
            await conn.OpenAsync().ConfigureAwait(false);

            var cmd = conn.CreateCommand();
            await using (cmd.ConfigureAwait(false))
            {
                cmd.CommandTimeout = commandTimeoutSeconds;
                cmd.CommandText =
                    $"CREATE TABLE IF NOT EXISTS \"{configuration.TableName}\" (" +
                    $"\"RowId\" TEXT NOT NULL DEFAULT (lower(hex(randomblob(16)))), " +
                    $"\"SecretKey\" TEXT NOT NULL, " +
                    $"\"SecretValue\" TEXT NOT NULL, " +
                    $"\"Version\" INTEGER NOT NULL DEFAULT 1, " +
                    $"\"SecretType\" TEXT NOT NULL DEFAULT 'Password', " +
                    $"\"Description\" TEXT NULL, " +
                    $"\"ExpiresAt\" TEXT NULL, " +
                    $"\"CreateDate\" TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now')), " +
                    $"\"ModifyDate\" TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now')), " +
                    $"\"IsCurrent\" INTEGER NOT NULL DEFAULT 1, " +
                    $"\"IsDeleted\" INTEGER NOT NULL DEFAULT 0, " +
                    $"CONSTRAINT \"PK_{configuration.TableName}\" PRIMARY KEY (\"RowId\")" +
                    $"); " +
                    // Why: Partial unique index enforces one current, non-deleted row per SecretKey —
                    // matching the SQL Server version-on-write constraint (IsCurrent=1 AND IsDeleted=0).
                    $"CREATE UNIQUE INDEX IF NOT EXISTS \"IX_{configuration.TableName}_Current\" " +
                    $"ON \"{configuration.TableName}\" (\"SecretKey\") " +
                    $"WHERE \"IsCurrent\" = 1 AND \"IsDeleted\" = 0";

                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

                SqliteSecretManagerLogger.TableCreated(_logger, configuration.TableName, configuration.DataSource);
                return GenericResult.Success();
            }
        }
    }

    /// <inheritdoc/>
    public Task<IGenericResult<ISecretManager>> CreateSecretManager(IGenericConfiguration configuration)
    {
        SqliteSecretManagerLogger.TraceCreateSecretManagerGenericEntry(_logger);

        if (configuration is SqliteSecretManagerConfiguration typed)
            return CreateSecretManagerInternal(typed, typed.Name);

        var actualType = configuration?.GetType().Name ?? LogPlaceholders.NullName;
        return Task.FromResult<IGenericResult<ISecretManager>>(
            GenericResult<ISecretManager>.Failure(
                SqliteSecretManagerLogger.InvalidConfigurationType(
                    _logger,
                    nameof(SqliteSecretManagerConfiguration),
                    actualType)));
    }

    #region IServiceFactory Implementation

#pragma warning disable VSTHRD002

    /// <inheritdoc/>
    public override IGenericResult<ISecretManager> Create(SqliteSecretManagerConfiguration configuration)
    {
        return CreateSecretManager(configuration).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    IGenericResult<ISecretManager> IServiceFactory<ISecretManager>.Create(IGenericConfiguration configuration)
    {
        return CreateSecretManager(configuration).GetAwaiter().GetResult();
    }



#pragma warning restore VSTHRD002

    #endregion
}
