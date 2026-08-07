using System;
using System.Globalization;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Sqlite.Configuration;
using ReferenceSecretManagers.Sqlite.Logging;
using ReferenceSecretManagers.Sqlite.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.SecretManagers.Sqlite.Commands;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.Sqlite;

/// <summary>
/// Factory for creating <see cref="SqliteSecretManager"/> instances.
/// </summary>
/// <remarks>
/// Opens a transient connection to verify the data source is reachable and ensures
/// the secrets table exists before returning the manager. The manager itself opens
/// per-operation connections via <c>SqliteExecutionContext.CreateConnection()</c>.
/// </remarks>
public sealed class SqliteSecretManagerFactory : ISqliteSecretManagerFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<SqliteSecretManagerFactory> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteSecretManagerFactory"/> class.
    /// </summary>
    /// <param name="loggerFactory">The logger factory for creating service loggers.</param>
    public SqliteSecretManagerFactory(ILoggerFactory? loggerFactory)
    {
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger<SqliteSecretManagerFactory>();
    }

    /// <inheritdoc/>
    public Task<IGenericResult<ISecretManager>> CreateSecretManager(SqliteSecretManagerConfiguration configuration)
        // Why: Direct typed-body path has no SecretManagerConfiguration header — name not available.
        => CreateSecretManagerInternal(configuration, string.Empty);

    private async Task<IGenericResult<ISecretManager>> CreateSecretManagerInternal(
        SqliteSecretManagerConfiguration configuration, string secretManagerName)
    {
        SqliteSecretManagerLogger.TraceCreateSecretManagerEntry(_logger);

        var effectiveName = secretManagerName.Length > 0 ? secretManagerName : configuration.SecretManagerId.ToString();

        try
        {
            SqliteSecretManagerLogger.CreatingSecretManager(_logger, effectiveName);

            var ensureResult = await EnsureTableExists(configuration).ConfigureAwait(false);
            if (ensureResult.IsFailure)
            {
                return ensureResult.ToNewResult<ISecretManager>();
            }

            var serviceLogger = _loggerFactory.CreateLogger<SqliteSecretManager>();
            var service = new SqliteSecretManager(serviceLogger, configuration, effectiveName);

            SqliteSecretManagerLogger.SecretManagerCreated(_logger, effectiveName);
            return GenericResult<ISecretManager>.Success(service);
        }
        catch (Exception ex)
        {
            return GenericResult<ISecretManager>.Failure(
                SqliteSecretManagerLogger.CreationFailed(_logger, ex, ex.Message));
        }
    }

    private async Task<IGenericResult> EnsureTableExists(SqliteSecretManagerConfiguration configuration)
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
                cmd.CommandTimeout = configuration.CommandTimeoutSeconds;
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

        // Why: After config-split DefaultServiceProvider.CreateFromParentConfig passes the composed
        // SecretManagerConfiguration header (with Configuration = SqliteSecretManagerConfiguration).
        // Extract the typed body and name from the header.
        if (configuration is SecretManagerConfiguration header
            && header.Configuration is SqliteSecretManagerConfiguration typedBody)
            return CreateSecretManagerInternal(typedBody, header.Name);

        if (configuration is SqliteSecretManagerConfiguration directConfig)
            return CreateSecretManagerInternal(directConfig, string.Empty);

        var actualType = configuration?.GetType().Name ?? "null";
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
    public IGenericResult<ISecretManager> Create(SqliteSecretManagerConfiguration configuration)
    {
        return CreateSecretManager(configuration).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    IGenericResult<ISecretManager> IServiceFactory<ISecretManager>.Create(IGenericConfiguration configuration)
    {
        return CreateSecretManager(configuration).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    IGenericResult<IGenericService> IServiceFactory.Create(IGenericConfiguration configuration)
    {
        var result = ((IServiceFactory<ISecretManager>)this).Create(configuration);

        if (!result.IsSuccess)
            return result.ToNewResult<IGenericService>();

        return result.ToNewResult((IGenericService)result.Value!);
    }

    /// <inheritdoc/>
    IGenericResult<T> IServiceFactory.Create<T>(IGenericConfiguration configuration)
    {
        var result = ((IServiceFactory)this).Create(configuration);
        if (!result.IsSuccess)
            return result.ToNewResult<T>();

        if (result.Value is T typedService)
            return result.ToNewResult(typedService);

        return GenericResult<T>.Failure(SqliteSecretManagerLogger.ServiceTypeMismatch(_logger, typeof(T).Name));
    }

#pragma warning restore VSTHRD002

    #endregion
}
