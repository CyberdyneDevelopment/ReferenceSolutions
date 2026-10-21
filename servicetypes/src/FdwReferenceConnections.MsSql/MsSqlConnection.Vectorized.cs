using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Abstractions.Logging;
using Fdw.Services.Connections.MsSql.Logging;
using FdwReferenceConnections.MsSql.Logging;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace FdwReferenceConnections.MsSql;

/// <summary>
/// SQL Server 2025 holds vectors, so this connection is vectorized (FDW-1354): it answers the rows nearest a vector, with
/// <c>VECTOR_DISTANCE</c> over a <c>vector</c> column.
/// </summary>
public sealed partial class MsSqlConnection : IVectorizedDataConnection
{
    /// <inheritdoc/>
    public async Task<IGenericResult<T>> Execute<T>(
        IVectorizedDataCommand<T> command, IDataContainer container, CancellationToken cancellationToken = default)
    {
        var translator = MsSqlVectorizedDataCommandTranslators.ByName(command.CommandType);
        if (translator == MsSqlVectorizedDataCommandTranslators.NotFound)
            return GenericResult<T>.Failure(ConnectionLogger.TranslatorNotFound(Logger, command.CommandType));

        // Why only Nearest: it is the one vectorized command, and its answer is rows with a distance. A command added to
        // the family is refused here, by name, until the connection reads its answer.
        if (command is not INearestCommand nearest)
            return GenericResult<T>.Failure(MsSqlVectorizedLogger.CommandNotRead(_logger, command.CommandType));

        var translated = await translator.Translate(command, container, cancellationToken).ConfigureAwait(false);
        if (!translated.IsSuccess || translated.Value is not { } sqlCommand)
        {
            return translated.Messages.Count > 0
                ? translated.ToNewResult<T>()
                : GenericResult<T>.Failure(ConnectionLogger.TranslationFailed(Logger));
        }

        var read = await ReadNearest(sqlCommand, nearest, container, cancellationToken).ConfigureAwait(false);
        return read.IsSuccess && read.Value is { } answer ? GenericResult<T>.Success((T)answer) : read.ToNewResult<T>();
    }

    // Runs the search as every read runs, with the caller's session context, and reads each row as the command's row type
    // and the last column as its distance (the translator always puts the distance last).
    private async Task<IGenericResult<object>> ReadNearest(
        SqlCommand command, INearestCommand nearest, IStorageContainer container, CancellationToken cancellationToken)
    {
        // What was asked to run, before the session context can be put in front of it (see Execute<T>).
        var commandText = command.CommandText;
        var parameterCount = command.Parameters.Count;
        var placement = SessionContextPlacement.Separate;

        MsSqlConnectionLogger.TraceExecuteEntry(Logger, commandText);

        try
        {
            var sqlConnection = CreatePooledConnection();
            await using (sqlConnection.ConfigureAwait(false))
            {
                await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
                command.Connection = sqlConnection;

                var placed = await PlaceSessionContext(sqlConnection, command, cancellationToken).ConfigureAwait(false);
                if (!placed.IsSuccess || placed.Value is not { } placedContext)
                    return placed.ToNewResult<object>();

                placement = placedContext;
                MsSqlConnectionLogger.ExecutingSqlCommand(Logger, commandText, parameterCount);

                using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                placement.Applied(sqlConnection);
                return await ReadNearestRows(reader, nearest, container, _logger, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (SqlException ex)
        {
            MsSqlConnectionLogger.SqlExecutionError(Logger, commandText, ex.Message, ex.Number);
            return GenericResult<object>.Failure(placement.FailureMessages(_logger, ex, commandText));
        }
        catch (Exception ex)
        {
            MsSqlConnectionLogger.ExecutionException(Logger, commandText, ex.Message);
            return GenericResult<object>.Failure(MsSqlConnectionLogger.SqlExecutionFailedWithMessage(_logger, ex, commandText));
        }
    }

    private static async Task<IGenericResult<object>> ReadNearestRows(
        SqlDataReader reader, INearestCommand nearest, IStorageContainer container, ILogger logger, CancellationToken cancellationToken)
    {
        var rows = new List<object>();
        var distances = new List<double>();

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var row = MapReaderRowToObject(reader, nearest.RowType, container, logger);
            if (!row.IsSuccess || row.Value is not { } value)
                return row;

            rows.Add(value);
            distances.Add(reader.GetDouble(reader.FieldCount - 1));
        }

        MsSqlVectorizedLogger.NearestRead(logger, container.Name, rows.Count);
        return GenericResult<object>.Success(nearest.Results(rows, distances));
    }
}
