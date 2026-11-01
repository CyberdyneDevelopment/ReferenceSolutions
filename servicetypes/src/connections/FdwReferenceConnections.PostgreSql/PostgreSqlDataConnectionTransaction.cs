using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.PostgreSql.Logging;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace FdwReferenceConnections.PostgreSql;

/// <summary>
/// A PostgreSQL unit of work: a connection of its own with a transaction open on it. PostgreSQL transactions belong to the
/// connection, so every command the wrapped <see cref="PostgreSqlConnection"/> runs is inside the transaction, with the
/// translator lookup, the expansion of a command with no translator, the row mapping and the binary COPY the connection
/// already has. Nothing here repeats them.
/// </summary>
/// <remarks>
/// Created only by <see cref="PostgreSqlConnection.BeginTransaction"/>. Disposing a unit that was neither committed nor
/// rolled back rolls it back, so an exit path that skips both never commits by accident.
/// </remarks>
internal sealed class PostgreSqlDataConnectionTransaction : IDataConnectionTransaction
{
    private readonly PostgreSqlConnection _connection;
    private readonly NpgsqlTransaction _transaction;
    private readonly ILogger _logger;
    private readonly string _connectionName;
    private bool _ended;
    private bool _disposed;

    internal PostgreSqlDataConnectionTransaction(
        PostgreSqlConnection connection,
        NpgsqlTransaction transaction,
        ILogger logger,
        string connectionName)
    {
        _connection = connection;
        _transaction = transaction;
        _logger = logger;
        _connectionName = connectionName;
    }

    /// <inheritdoc />
    public bool IsActive => !_ended && !_disposed;

    /// <inheritdoc />
    public Task<IGenericResult<T>> Execute<T>(
        IDataCommand command,
        IDataContainer container,
        CancellationToken cancellationToken = default)
        => IsActive
            ? _connection.Execute<T>(command, container, cancellationToken)
            : Task.FromResult(GenericResult<T>.Failure(PostgreSqlConnectionLog.UnitOfWorkNoLongerActive(_logger, _connectionName)));

    /// <inheritdoc />
    public Task<IGenericResult> Execute(
        IDataCommand command,
        IDataContainer container,
        CancellationToken cancellationToken = default)
        => IsActive
            ? _connection.Execute(command, container, cancellationToken)
            : Task.FromResult<IGenericResult>(GenericResult.Failure(PostgreSqlConnectionLog.UnitOfWorkNoLongerActive(_logger, _connectionName)));

    /// <inheritdoc />
    public async Task<IGenericResult> Commit(CancellationToken cancellationToken = default)
    {
        if (!IsActive)
            return GenericResult.Failure(PostgreSqlConnectionLog.UnitOfWorkNoLongerActive(_logger, _connectionName));

        try
        {
            await _transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            _ended = true;
            PostgreSqlConnectionLog.UnitOfWorkCommitted(_logger, _connectionName);
            return GenericResult.Success();
        }
        catch (Exception ex)
        {
            return GenericResult.Failure(PostgreSqlConnectionLog.UnitOfWorkFailed(_logger, ex, "commit", _connectionName));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Rollback(CancellationToken cancellationToken = default)
    {
        if (!IsActive)
            return GenericResult.Failure(PostgreSqlConnectionLog.UnitOfWorkNoLongerActive(_logger, _connectionName));

        try
        {
            await _transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            _ended = true;
            PostgreSqlConnectionLog.UnitOfWorkRolledBack(_logger, _connectionName);
            return GenericResult.Success();
        }
        catch (Exception ex)
        {
            return GenericResult.Failure(PostgreSqlConnectionLog.UnitOfWorkFailed(_logger, ex, "roll back", _connectionName));
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        try
        {
            if (!_ended)
                await _transaction.RollbackAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Disposal must not throw, and the failure must still be seen: the connection is closed next, which
            // ends the transaction on the server.
            PostgreSqlConnectionLog.UnitOfWorkDisposeRollbackFailed(_logger, ex, _connectionName);
        }
        finally
        {
            _disposed = true;
            await _transaction.DisposeAsync().ConfigureAwait(false);
            _connection.Dispose();
        }
    }
}
