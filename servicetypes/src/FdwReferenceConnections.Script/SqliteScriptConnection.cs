using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Sqlite.Logging;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Scripts;
using FdwReferenceConnections.Sqlite;
using Microsoft.Data.Sqlite;

namespace FdwReferenceConnections.Script;

/// <summary>
/// SQLite takes a script (FDW-1447): this wraps a <see cref="FdwReferenceConnections.Sqlite.SqliteConnection"/> and runs text a data engineer
/// wrote with named parameters, as a query that answers its rows or as a command that answers the rows it affected. Values are
/// driver parameters, never part of the text.
/// </summary>
public sealed class SqliteScriptConnection : IScriptedDataConnection
{
    private readonly FdwReferenceConnections.Sqlite.SqliteConnection _connection;

    /// <summary>Initializes a new instance of the <see cref="SqliteScriptConnection"/> class.</summary>
    /// <param name="connection">The SQLite connection the script runs on.</param>
    public SqliteScriptConnection(FdwReferenceConnections.Sqlite.SqliteConnection connection)
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

    private static SqliteCommand ScriptStatement(IScriptedDataCommand command)
    {
        var statement = new SqliteCommand(command.Text);
        ScriptedRows.Bind(statement, command.Parameters, "@");
        return statement;
    }

    private async Task<IGenericResult<IReadOnlyList<IDictionary<string, object?>>>> ReadScript(
        IScriptReadCommand command, CancellationToken cancellationToken)
    {
        var logger = _connection.ScriptLogger;
        var name = _connection.ScriptConnectionName;
        try
        {
            var adoConnection = _connection.CreateAdoConnection();
            await using (adoConnection.ConfigureAwait(false))
            {
                await adoConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
                using var statement = ScriptStatement(command);
                statement.Connection = adoConnection;
                SqliteConnectionLog.ExecutingCommand(logger, name, command.Text, statement.Parameters.Count);

                using var reader = await statement.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                return await ScriptedRows.Read(reader, command.Take, logger, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (SqliteException ex)
        {
            return GenericResult<IReadOnlyList<IDictionary<string, object?>>>.Failure(
                ExceptionResultExtensions.FlattenException(ex).Prepend(SqliteConnectionLog.ExecutionFailedWithException(logger, ex, name)));
        }
        catch (Exception ex)
        {
            return GenericResult<IReadOnlyList<IDictionary<string, object?>>>.Failure(
                SqliteConnectionLog.ExecutionFailedWithException(logger, ex, name));
        }
    }

    private async Task<IGenericResult<int>> RunScript(IScriptRunCommand command, CancellationToken cancellationToken)
    {
        var logger = _connection.ScriptLogger;
        var name = _connection.ScriptConnectionName;
        try
        {
            var adoConnection = _connection.CreateAdoConnection();
            await using (adoConnection.ConfigureAwait(false))
            {
                await adoConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
                using var statement = ScriptStatement(command);
                statement.Connection = adoConnection;
                SqliteConnectionLog.ExecutingCommand(logger, name, command.Text, statement.Parameters.Count);

                var affected = await statement.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                SqliteConnectionLog.CommandCompleted(logger, name, affected);
                return GenericResult<int>.Success(affected);
            }
        }
        catch (SqliteException ex)
        {
            return GenericResult<int>.Failure(
                ExceptionResultExtensions.FlattenException(ex).Prepend(SqliteConnectionLog.ExecutionFailedWithException(logger, ex, name)));
        }
        catch (Exception ex)
        {
            return GenericResult<int>.Failure(SqliteConnectionLog.ExecutionFailedWithException(logger, ex, name));
        }
    }
}
