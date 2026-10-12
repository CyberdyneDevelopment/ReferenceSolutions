using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.MsSql.Logging;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Fdw.Services.Connections.MsSql.ErrorHandlers;


namespace FdwReferenceConnections.MsSql;

/// <summary>
/// Holds an open <see cref="SqlConnection"/> and a live <see cref="SqlTransaction"/>.
/// All Execute calls run inside that transaction. Commit or Rollback to end the scope.
/// </summary>
/// <remarks>
/// Created exclusively by <see cref="MsSqlConnection.BeginTransaction"/>. The
/// <c>MsSqlConnection</c> itself continues to pool-per-call for non-transactional
/// operations; only the commands submitted through this scope share the one connection.
/// </remarks>
[ExcludeFromCodeCoverage] // Excluded: requires SQL Server connection
internal sealed class MsSqlDataConnectionTransaction : IDataConnectionTransaction
{
    private readonly SqlConnection _connection;
    private SqlTransaction _transaction;
    private readonly ILogger _logger;
    private readonly string _connectionName;
    private readonly int _maxSerialInsert;
    private readonly BulkCopyWriter _bulkCopy;
    private bool _disposed;

    /// <summary>Copies a planned bulk insert on the unit of work's connection and transaction; the default is <see cref="MsSqlBulkCopy.Write"/>.</summary>
    internal delegate Task<int> BulkCopyWriter(
        MsSqlBulkCopyPlan plan, SqlConnection connection, SqlTransaction? transaction, CancellationToken cancellationToken);

    internal MsSqlDataConnectionTransaction(
        SqlConnection connection,
        SqlTransaction transaction,
        ILogger logger,
        string connectionName,
        int maxSerialInsert)
        : this(connection, transaction, logger, connectionName, maxSerialInsert, MsSqlBulkCopy.Write)
    {
    }

    // The writer is a parameter so the offline tests can see that a bulk insert above the limit is copied on this unit of work's
    // connection and transaction without a SQL Server to copy into.
    internal MsSqlDataConnectionTransaction(
        SqlConnection connection,
        SqlTransaction transaction,
        ILogger logger,
        string connectionName,
        int maxSerialInsert,
        BulkCopyWriter bulkCopy)
    {
        _connection = connection;
        _transaction = transaction;
        _logger = logger;
        _connectionName = connectionName;
        _maxSerialInsert = maxSerialInsert;
        _bulkCopy = bulkCopy;
    }

    /// <inheritdoc />
    public bool IsActive => !_disposed && _transaction != null;

    /// <inheritdoc />
    public async Task<IGenericResult<T>> Execute<T>(
        IDataCommand command,
        IDataContainer container,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
            return GenericResult<T>.Failure(MsSqlConnectionLogger.ExecutionFailed(_logger));

        // A command that asks for the bulk route takes it inside this unit of work, on its connection and transaction (FDW-1152).
        if (MsSqlBulkRoute.Takes(command, container, _maxSerialInsert))
            return await ExecuteBulk<T>(command, container, cancellationToken).ConfigureAwait(false);

        var translator = MsSqlDataCommandTranslators.ByName(command.CommandType);
        if (string.Equals(translator.Name, "NotFound", StringComparison.Ordinal) ||
            !string.Equals(translator.Name, command.CommandType, StringComparison.Ordinal))
        {
            return GenericResult<T>.Failure(
                MsSqlConnectionLogger.ExecutionFailed(_logger));
        }

        // Why (Stage 3): the container is built complete — its Schema is a synchronous projection over
        // its IDataField child nodes, so there is no materialization step. The container is passed
        // directly to the translator and the native execute (same fix applied to ConnectionBase.Execute).
        var translationResult = await translator.Translate(command, container, cancellationToken)
            .ConfigureAwait(false);
        if (!translationResult.IsSuccess || translationResult.Value == null)
        {
            return translationResult.Messages.Any()
                ? translationResult.ToNewResult<T>()
                : GenericResult<T>.Failure(MsSqlConnectionLogger.ExecutionFailed(_logger));
        }

        return await ExecuteNative<T>(translationResult.Value, container, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Execute(
        IDataCommand command,
        IDataContainer container,
        CancellationToken cancellationToken = default)
    {
        var result = await Execute<object>(command, container, cancellationToken)
            .ConfigureAwait(false);
        return result.IsSuccess
            ? GenericResult.Success()
            : result;
    }

    // The bulk route inside the unit of work: the rows are copied on the unit's own connection with its live transaction, so a
    // rollback undoes them with the rest. The session context was set once when the unit began.
    private async Task<IGenericResult<T>> ExecuteBulk<T>(IDataCommand command, IDataContainer container, CancellationToken cancellationToken)
    {
        var plan = MsSqlBulkRoute.Plan(command, container, _logger, _connectionName, _maxSerialInsert, inTransaction: true);
        if (!plan.IsSuccess || plan.Value is null)
            return plan.ToNewResult<T>();

        var copyText = $"BULK INSERT INTO {plan.Value.Destination}";
        try
        {
            var copied = await _bulkCopy(plan.Value, _connection, _transaction, cancellationToken).ConfigureAwait(false);
            MsSqlConnectionLogger.SqlCommandExecuted(_logger, copyText, copied);
            return GenericResult<T>.Success(MsSqlConnection.ConvertScalarResult<T>(copied));
        }
        catch (SqlException ex)
        {
            MsSqlConnectionLogger.SqlExecutionError(_logger, copyText, ex.Message, ex.Number);
            return GenericResult<T>.Failure(SqlErrorHandlers.FailureMessages(_logger, ex, copyText));
        }
        catch (Exception ex)
        {
            MsSqlConnectionLogger.ExecutionException(_logger, copyText, ex.Message);
            return GenericResult<T>.Failure(MsSqlConnectionLogger.BulkCopyFailed(_logger, ex));
        }
    }

    private async Task<IGenericResult<T>> ExecuteNative<T>(
        SqlCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken)
    {
        // Why: The transaction owns the open connection. Assign both so the command runs
        // inside the transaction — without this the command would start a new connection.
        command.Connection = _connection;
        command.Transaction = _transaction;

        try
        {
            // A configuration save or retire answers with the key values of the rows it touched.
            if (typeof(T) == typeof(ConfigurationWriteResult))
            {
                using var writeReader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                var writeResult = await MsSqlConnection.ReadWriteResult(writeReader, _logger, cancellationToken).ConfigureAwait(false);
                return GenericResult<T>.Success((T)(object)writeResult);
            }

            var isQuery = command.CommandText.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase);
            if (!isQuery)
            {
                var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken)
                    .ConfigureAwait(false);
                return GenericResult<T>.Success(MsSqlConnection.ConvertScalarResult<T>(rowsAffected));
            }

            using var reader = await command.ExecuteReaderAsync(cancellationToken)
                .ConfigureAwait(false);
            return await MsSqlConnection.MapReaderToTypeInternal<T>(reader, container, _logger, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (SqlException ex)
        {
            MsSqlConnectionLogger.SqlExecutionError(_logger, command.CommandText, ex.Message, ex.Number);
            return GenericResult<T>.Failure(SqlErrorHandlers.FailureMessages(_logger, ex, command.CommandText));
        }
        catch (Exception ex)
        {
            MsSqlConnectionLogger.ExecutionException(_logger, command.CommandText, ex.Message);
            return GenericResult<T>.Failure(
                MsSqlConnectionLogger.SqlExecutionFailedWithMessage(_logger, ex, command.CommandText));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Commit(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            return GenericResult.Failure(MsSqlConnectionLogger.ExecutionFailed(_logger));

        try
        {
            await _transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            MsSqlConnectionLogger.TransactionCommitted(_logger, _connectionName);
            return GenericResult.Success();
        }
        catch (Exception ex)
        {
            return GenericResult.Failure(
                MsSqlConnectionLogger.TransactionCommitFailed(_logger, _connectionName, ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Rollback(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            return GenericResult.Success(); // Already cleaned up — rollback is implicitly complete.

        try
        {
            await _transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            MsSqlConnectionLogger.TransactionRolledBack(_logger, _connectionName);
            return GenericResult.Success();
        }
        catch (Exception ex)
        {
            // Why: An explicit Rollback() that throws must surface to the caller — swallowing it
            // would hide a transaction that may not have been undone. (Implicit rollback on dispose
            // is handled separately in DisposeAsync, which SQL Server treats as a rollback anyway.)
            return GenericResult.Failure(
                MsSqlConnectionLogger.TransactionRollbackFailed(_logger, _connectionName, ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Why: Implicit rollback on dispose ensures exception paths that skip explicit
        // Rollback() are still safe — the transaction is never silently committed.
        try
        {
            await _transaction.RollbackAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Why: transaction may already be in a terminal state (committed, rolled back, or
            // connection broken). SQL Server cleans up on its end in all these cases, so no
            // further action is possible here. Logged at Debug so the exception is observed
            // without alarming operators with expected disposal-path noise.
            MsSqlConnectionLogger.TransactionDisposeRollbackIgnored(_logger, ex, _connectionName, ex.Message);
        }
        finally
        {
            _transaction.Dispose();
            await _connection.DisposeAsync().ConfigureAwait(false);
        }
    }
}
