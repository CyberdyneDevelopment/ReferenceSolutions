using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.MsSql.Logging;
using Fdw.Services.Connections.Scripts;
using FdwReferenceConnections.MsSql;
using Microsoft.Data.SqlClient;

namespace FdwReferenceConnections.Script;

/// <summary>
/// SQL Server takes a script (FDW-1447): this wraps a <see cref="MsSqlConnection"/> and runs text a data engineer wrote with
/// named parameters, as a query that answers its rows or as a command that answers the rows it affected.
/// </summary>
/// <remarks>
/// A script runs as every read and write does: on one of the connection's pooled connections, with the caller's session
/// context placed first, so row-level security holds for it. Its values are SQL parameters and never part of the text. It runs
/// in the database the connection is configured with and is one batch (no <c>GO</c>).
/// </remarks>
public sealed class MsSqlScriptConnection : IScriptedDataConnection
{
    private readonly MsSqlConnection _connection;

    /// <summary>Initializes a new instance of the <see cref="MsSqlScriptConnection"/> class.</summary>
    /// <param name="connection">The SQL Server connection the script runs on.</param>
    public MsSqlScriptConnection(MsSqlConnection connection)
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

    // The statement a script is: its text with its values as parameters. Internal so the tests can check what is sent.
    internal static SqlCommand ScriptStatement(IScriptedDataCommand command)
    {
        var statement = new SqlCommand(command.Text);
        ScriptedRows.Bind(statement, command.Parameters, "@");
        return statement;
    }

    private async Task<IGenericResult<IReadOnlyList<IDictionary<string, object?>>>> ReadScript(IScriptReadCommand command, CancellationToken cancellationToken)
    {
        using var statement = ScriptStatement(command);
        var text = command.Text;
        var logger = _connection.ScriptLogger;
        var placement = MsSqlConnection.SessionContextPlacement.Separate;

        try
        {
            var sqlConnection = _connection.CreatePooledConnection();
            await using (sqlConnection.ConfigureAwait(false))
            {
                await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
                statement.Connection = sqlConnection;

                var placed = await _connection.PlaceSessionContext(sqlConnection, statement, cancellationToken).ConfigureAwait(false);
                if (!placed.IsSuccess || placed.Value is not { } placedContext)
                    return placed.ToNewResult<IReadOnlyList<IDictionary<string, object?>>>();

                placement = placedContext;
                MsSqlConnectionLogger.ExecutingSqlCommand(logger, text, statement.Parameters.Count);

                using var reader = await statement.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                placement.Applied(sqlConnection);
                return await ScriptedRows.Read(reader, command.Take, logger, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (SqlException ex)
        {
            MsSqlConnectionLogger.SqlExecutionError(logger, text, ex.Message, ex.Number);
            return GenericResult<IReadOnlyList<IDictionary<string, object?>>>.Failure(placement.FailureMessages(logger, ex, text));
        }
        catch (Exception ex)
        {
            MsSqlConnectionLogger.ExecutionException(logger, text, ex.Message);
            return GenericResult<IReadOnlyList<IDictionary<string, object?>>>.Failure(MsSqlConnectionLogger.SqlExecutionFailedWithMessage(logger, ex, text));
        }
    }

    private async Task<IGenericResult<int>> RunScript(IScriptRunCommand command, CancellationToken cancellationToken)
    {
        using var statement = ScriptStatement(command);
        var text = command.Text;
        var logger = _connection.ScriptLogger;
        var placement = MsSqlConnection.SessionContextPlacement.Separate;

        try
        {
            var sqlConnection = _connection.CreatePooledConnection();
            await using (sqlConnection.ConfigureAwait(false))
            {
                await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
                statement.Connection = sqlConnection;

                var placed = await _connection.PlaceSessionContext(sqlConnection, statement, cancellationToken).ConfigureAwait(false);
                if (!placed.IsSuccess || placed.Value is not { } placedContext)
                    return placed.ToNewResult<int>();

                placement = placedContext;
                MsSqlConnectionLogger.ExecutingSqlCommand(logger, text, statement.Parameters.Count);

                var affected = await statement.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                placement.Applied(sqlConnection);
                MsSqlConnectionLogger.SqlCommandExecuted(logger, text, affected);
                return GenericResult<int>.Success(affected);
            }
        }
        catch (SqlException ex)
        {
            MsSqlConnectionLogger.SqlExecutionError(logger, text, ex.Message, ex.Number);
            return GenericResult<int>.Failure(placement.FailureMessages(logger, ex, text));
        }
        catch (Exception ex)
        {
            MsSqlConnectionLogger.ExecutionException(logger, text, ex.Message);
            return GenericResult<int>.Failure(MsSqlConnectionLogger.SqlExecutionFailedWithMessage(logger, ex, text));
        }
    }
}
