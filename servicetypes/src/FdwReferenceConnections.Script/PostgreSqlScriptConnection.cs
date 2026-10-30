using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.PostgreSql.Logging;
using Fdw.Services.Connections.Scripts;
using FdwReferenceConnections.PostgreSql;
using Npgsql;

namespace FdwReferenceConnections.Script;

/// <summary>
/// PostgreSQL takes a script (FDW-1447): this wraps a <see cref="PostgreSqlConnection"/> and runs text a data engineer wrote with
/// named parameters, as a query that answers its rows or as a command that answers the rows it affected. Values are driver
/// parameters, never part of the text, and the script is one command on the connection's database.
/// </summary>
public sealed class PostgreSqlScriptConnection : IScriptedDataConnection
{
    private readonly PostgreSqlConnection _connection;

    /// <summary>Initializes a new instance of the <see cref="PostgreSqlScriptConnection"/> class.</summary>
    /// <param name="connection">The PostgreSQL connection the script runs on.</param>
    public PostgreSqlScriptConnection(PostgreSqlConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <inheritdoc/>
    public Task<IGenericResult<T>> Execute<T>(IScriptedDataCommand<T> command, CancellationToken cancellationToken = default)
        => ScriptedCommands.Route(
            command,
            read => ReadScript(read, cancellationToken),
            run => RunScript(run, cancellationToken),
            _connection.ScriptLogger);

    private static NpgsqlCommand ScriptStatement(IScriptedDataCommand command)
    {
        var statement = new NpgsqlCommand(command.Text);
        ScriptedRows.Bind(statement, command.Parameters, "@");
        return statement;
    }

    private async Task<IGenericResult<IReadOnlyList<IDictionary<string, object?>>>> ReadScript(
        IScriptReadCommand command, CancellationToken cancellationToken)
    {
        var logger = _connection.ScriptLogger;
        try
        {
            using var statement = ScriptStatement(command);
            await OpenForScript(statement, command.Text, cancellationToken).ConfigureAwait(false);

            using var reader = await statement.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            return await ScriptedRows.Read(reader, command.Take, logger, cancellationToken).ConfigureAwait(false);
        }
        catch (NpgsqlException ex)
        {
            return GenericResult<IReadOnlyList<IDictionary<string, object?>>>.Failure(Failed(ex, command.Text));
        }
        catch (Exception ex)
        {
            return GenericResult<IReadOnlyList<IDictionary<string, object?>>>.Failure(
                PostgreSqlConnectionLog.SqlExecutionFailed(logger, ex, command.Text));
        }
    }

    private async Task<IGenericResult<int>> RunScript(IScriptRunCommand command, CancellationToken cancellationToken)
    {
        var logger = _connection.ScriptLogger;
        try
        {
            using var statement = ScriptStatement(command);
            await OpenForScript(statement, command.Text, cancellationToken).ConfigureAwait(false);

            var affected = await statement.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            PostgreSqlConnectionLog.SqlCommandExecuted(logger, command.Text, affected);
            return GenericResult<int>.Success(affected);
        }
        catch (NpgsqlException ex)
        {
            return GenericResult<int>.Failure(Failed(ex, command.Text));
        }
        catch (Exception ex)
        {
            return GenericResult<int>.Failure(PostgreSqlConnectionLog.SqlExecutionFailed(logger, ex, command.Text));
        }
    }

    // What a failed script says: the connection's failure and the exception chain, and, when the script outran the connection's
    // command timeout, first the coded message that names the connection and the seconds.
    private IEnumerable<IGenericMessage> Failed(NpgsqlException exception, string text)
    {
        var messages = ExceptionResultExtensions.FlattenException(exception)
            .Prepend(PostgreSqlConnectionLog.QueryFailed(_connection.ScriptLogger, exception, _connection.ScriptConnectionName));
        return PostgreSqlConnection.OutranCommandTimeout(exception) ? messages.Prepend(_connection.CommandTimedOut(text)) : messages;
    }

    // The connection is opened on first use, as every other command on it does, and the script runs on it.
    private async Task OpenForScript(NpgsqlCommand statement, string text, CancellationToken cancellationToken)
    {
        var inner = _connection.InnerConnection;
        var logger = _connection.ScriptLogger;
        if (inner.State != ConnectionState.Open)
        {
            await inner.OpenAsync(cancellationToken).ConfigureAwait(false);
            PostgreSqlConnectionLog.ConnectionOpened(logger, _connection.ScriptConnectionName);
        }

        statement.Connection = inner;
        PostgreSqlConnectionLog.ExecutingSqlCommand(logger, text, statement.Parameters.Count);
    }
}
