using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Data.Sqlite.Logging;
using Fdw.Data.Sqlite.Results;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using SqliteAdoConnection = Microsoft.Data.Sqlite.SqliteConnection;

using Fdw.Services.Connections;
using Fdw.Services.Connections.Sqlite;

using Fdw.Services.Connections.Sqlite.Authentication;

using Fdw.Services.Connections.Sqlite.Commands;

using Fdw.Services.Connections.Sqlite.Validation;

namespace ReferenceConnections.Sqlite;

/// <summary>
/// Holds an open <see cref="SqliteAdoConnection"/> and a live <see cref="SqliteTransaction"/>.
/// All Execute calls run inside that transaction. Commit or Rollback to end the scope.
/// </summary>
internal sealed class SqliteDataConnectionTransaction : IDataConnectionTransaction
{
    private readonly SqliteAdoConnection _connection;
    private readonly SqliteTransaction _transaction;
    private readonly ILogger _logger;
    private readonly string _connectionName;
    private bool _disposed;

    internal SqliteDataConnectionTransaction(
        SqliteAdoConnection connection,
        SqliteTransaction transaction,
        ILogger logger,
        string connectionName)
    {
        _connection = connection;
        _transaction = transaction;
        _logger = logger;
        _connectionName = connectionName;
    }

    /// <inheritdoc/>
    public bool IsActive => !_disposed && _transaction != null;

    /// <inheritdoc/>
    public async Task<IGenericResult<T>> Execute<T>(
        IDataCommand command,
        IDataContainer container,
        CancellationToken cancellationToken = default)
    {
        SqliteConnectionLog.TraceTransactionExecuteEntry(_logger, _connectionName);

        if (_disposed)
            return GenericResult<T>.Failure(
                SqliteDataResultCodes.ByName("TransactionDisposed"));

        var translator = SqliteDataCommandTranslators.ByName(command.CommandType);
        if (string.Equals(translator.Name, "_Empty", StringComparison.Ordinal))
            return GenericResult<T>.Failure(
                SqliteDataResultCodes.ByName("InvalidCommandType"));

        var translationResult = await translator.Translate(command, container, cancellationToken)
            .ConfigureAwait(false);

        if (!translationResult.IsSuccess || translationResult.Value == null)
        {
            return translationResult.Messages.Any()
                ? translationResult.ToNewResult<T>()
                : GenericResult<T>.Failure(SqliteDataResultCodes.ByName("ExecutionFailed"));
        }

        return await ExecuteNative<T>(translationResult.Value, container, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult> Execute(
        IDataCommand command,
        IDataContainer container,
        CancellationToken cancellationToken = default)
    {
        var result = await Execute<object>(command, container, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? GenericResult.Success() : result;
    }

    private async Task<IGenericResult<T>> ExecuteNative<T>(
        SqliteCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken)
    {
        command.Connection = _connection;
        command.Transaction = _transaction;

        try
        {
            var isQuery = command.CommandText.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase);

            if (!isQuery)
            {
                var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                return GenericResult<T>.Success(SqliteConnection.ConvertScalarResult<T>(rowsAffected));
            }

            using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            var mapped = await SqliteConnection.MapReaderToTypeInternal<T>(reader, container, _logger, cancellationToken).ConfigureAwait(false);
            return GenericResult<T>.Success(mapped);
        }
        catch (SqliteException ex)
        {
            return GenericResult<T>.Failure(
                SqliteConnectionLog.ExecutionFailedWithException(_logger, ex, _connectionName));
        }
        catch (Exception ex)
        {
            return GenericResult<T>.Failure(
                SqliteConnectionLog.ExecutionFailedWithException(_logger, ex, _connectionName));
        }
    }

    /// <inheritdoc/>
    public async Task<IGenericResult> Commit(CancellationToken cancellationToken = default)
    {
        SqliteConnectionLog.TraceTransactionCommitEntry(_logger, _connectionName);

        if (_disposed)
            return GenericResult.Failure(SqliteDataResultCodes.ByName("TransactionDisposed"));

        try
        {
            await _transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            SqliteConnectionLog.TransactionCommitted(_logger, _connectionName);
            return GenericResult.Success();
        }
        catch (Exception ex)
        {
            return GenericResult.Failure(
                SqliteConnectionLog.ExecutionFailedWithException(_logger, ex, _connectionName));
        }
    }

    /// <inheritdoc/>
    public async Task<IGenericResult> Rollback(CancellationToken cancellationToken = default)
    {
        SqliteConnectionLog.TraceTransactionRollbackEntry(_logger, _connectionName);

        if (_disposed)
            return GenericResult.Success(); // Already cleaned up — rollback is implicitly complete.

        try
        {
            await _transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            SqliteConnectionLog.TransactionRolledBack(_logger, _connectionName, "explicit rollback");
            return GenericResult.Success();
        }
        catch (Exception ex)
        {
            return GenericResult.Failure(
                SqliteConnectionLog.ExecutionFailedWithException(_logger, ex, _connectionName));
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            // Why: Implicit rollback on dispose ensures exception paths that skip explicit Rollback()
            // are still safe — the transaction is never silently committed.
            await _transaction.RollbackAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Why: transaction may already be committed or connection broken; SQLite cleans up on
            // its end. Log-and-swallow — disposal must not throw, but the exception must be observed.
            SqliteConnectionLog.DisposeRollbackFailed(_logger, ex, _connectionName);
        }
        finally
        {
            _transaction.Dispose();
            await _connection.DisposeAsync().ConfigureAwait(false);
        }
    }
}
