using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Abstractions.Logging;
using FdwReferenceConnections.MsSql.Logging;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace FdwReferenceConnections.MsSql;

/// <summary>
/// SQL Server runs a find itself (FDW-1361): its translator selects a match column for each searched field beside the
/// row, and this connection reads the row as the command's row type and the fields whose match column is 1.
/// </summary>
public sealed partial class MsSqlConnection
{
    private async Task<IGenericResult<T>> ExecuteFind<T>(IFindCommand find, IDataContainer container, CancellationToken cancellationToken)
    {
        var translator = GetTranslator(find.CommandType);
        if (string.Equals(translator.Name, "NotFound", StringComparison.Ordinal))
            return GenericResult<T>.Failure(ConnectionLogger.TranslatorNotFound(Logger, find.CommandType));

        var translated = await translator.Translate(find, container, cancellationToken).ConfigureAwait(false);
        if (!translated.IsSuccess || translated.Value is not { } sqlCommand)
        {
            return translated.Messages.Count > 0
                ? translated.ToNewResult<T>()
                : GenericResult<T>.Failure(ConnectionLogger.TranslationFailed(Logger));
        }

        var read = await ReadRows(sqlCommand, reader => ReadFindRows(reader, find, container, _logger, cancellationToken), cancellationToken).ConfigureAwait(false);
        return read.IsSuccess && read.Value is { } answer ? GenericResult<T>.Success((T)answer) : read.ToNewResult<T>();
    }

    private static async Task<IGenericResult<object>> ReadFindRows(
        SqlDataReader reader, IFindCommand find, IStorageContainer container, ILogger logger, CancellationToken cancellationToken)
    {
        var matchColumns = MatchColumnsOf(reader);
        var rows = new List<object>();
        var matchedFields = new List<IReadOnlyList<string>>();

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var row = MapReaderRowToObject(reader, find.RowType, container, logger);
            if (!row.IsSuccess || row.Value is not { } value)
                return row;

            rows.Add(WithoutMatchColumns(value));
            matchedFields.Add(MatchedFieldsOf(reader, matchColumns));
        }

        MsSqlFindLogger.FindRead(logger, container.Name, rows.Count);
        return GenericResult<object>.Success(find.Results(rows, matchedFields));
    }

    // The match columns are found by their prefix, once for the result set, so the connection needs to know neither how
    // many fields were searched nor which.
    private static List<(int Ordinal, string Field)> MatchColumnsOf(SqlDataReader reader)
    {
        var columns = new List<(int Ordinal, string Field)>();
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (FindMatchColumn.FieldOf(reader.GetName(i)) is { } field)
                columns.Add((i, field));
        }

        return columns;
    }

    // A match column is 1 when its field held the search term in this row (the translator's CASE returns an int).
    private static List<string> MatchedFieldsOf(SqlDataReader reader, List<(int Ordinal, string Field)> matchColumns)
    {
        var matched = new List<string>();
        foreach (var (ordinal, field) in matchColumns)
        {
            if (reader.GetInt32(ordinal) == 1)
                matched.Add(field);
        }

        return matched;
    }

    // Why the keys are removed: a row read as a dictionary holds every column the reader returned, and a match column is
    // the connection's measurement, not a field of the row.
    private static object WithoutMatchColumns(object row)
    {
        if (row is Dictionary<string, object?> dictionary)
        {
            foreach (var key in dictionary.Keys.Where(k => FindMatchColumn.FieldOf(k) is not null).ToList())
                dictionary.Remove(key);
        }

        return row;
    }
}
