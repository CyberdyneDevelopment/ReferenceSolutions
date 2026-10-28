using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.RowQuery;
using FdwReferenceConnections.FileSystem.Abstractions.Logging;
using FdwReferenceData.FileSystem;
using FdwReferenceData.FileSystem.Logging;
using Microsoft.Extensions.Logging;

namespace FdwReferenceConnections.FileSystem;

/// <summary>
/// Answers a <see cref="FileSystemFindCommand"/> over the rows a file holds: keeps the rows in which a searched field
/// contains the search term, in file order, and builds the <c>FindResult&lt;T&gt;</c> list the caller asked for.
/// </summary>
/// <remarks>
/// <c>FindResult&lt;T&gt;</c> lives in a package this project does not reference, so the result is built from the
/// requested type by reflection, the way the SQL Find translators read <c>FindCommand&lt;T&gt;</c>.
/// </remarks>
internal sealed class FileSystemFinder
{
    private const string FindResultTypeName = "Fdw.Commands.Data.FindResult`1";

    private readonly string _connectionName;
    private readonly ILogger _logger;

    public FileSystemFinder(string connectionName, ILogger logger)
    {
        _connectionName = connectionName;
        _logger = logger;
    }

    /// <summary>Keeps the rows in which a searched field contains the search term.</summary>
    /// <param name="command">The find command.</param>
    /// <param name="rows">The rows the file holds.</param>
    /// <returns>Each match with the fields that matched; or the failure that stopped the search.</returns>
    public IGenericResult<IReadOnlyList<FoundRow>> Search(
        FileSystemFindCommand command, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        var containerName = command.Container.Name;
        if (command.MaxResults < 0)
            return GenericResult<IReadOnlyList<FoundRow>>.Failure(
                FileSystemConnectionLog.FactoryValidationFailed(_logger, _connectionName, $"container '{containerName}': MaxResults cannot be negative"));

        var declared = command.Container.Schema.Fields;
        var searched = new List<string>();
        if (command.FieldNames is { Count: > 0 })
        {
            foreach (var name in command.FieldNames)
            {
                var field = declared.FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));
                if (field is null)
                    return GenericResult<IReadOnlyList<FoundRow>>.Failure(
                        FileSystemConfigurationWriteLog.FindFieldNotDeclared(_logger, _connectionName, containerName, name));
                searched.Add(field.Name);
            }
        }
        else
        {
            // The SQL kinds search the string columns when none are named; a file does the same.
            searched.AddRange(declared.Where(f => f.FieldType.ClrType == typeof(string)).Select(f => f.Name));
        }

        var comparison = command.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var found = new List<FoundRow>();
        foreach (var row in rows)
        {
            if (found.Count >= (command.MaxResults ?? int.MaxValue))
                break;

            var matched = searched
                .Where(name => row.TryGetValue(name, out var value)
                    && Convert.ToString(value, CultureInfo.InvariantCulture) is { } text
                    && text.Contains(command.SearchTerm, comparison))
                .ToList();
            if (matched.Count > 0)
                found.Add(new FoundRow(row, matched));
        }

        return GenericResult<IReadOnlyList<FoundRow>>.Success(found);
    }

    /// <summary>Builds the list of <c>FindResult&lt;T&gt;</c> the caller asked for from the matches.</summary>
    /// <typeparam name="T">The requested result type: a collection of <c>FindResult&lt;TRecord&gt;</c>.</typeparam>
    /// <param name="found">The matches.</param>
    /// <param name="container">The container the rows came from.</param>
    /// <returns>The result list; or the failure that stopped it.</returns>
    public IGenericResult<T> Answer<T>(IReadOnlyList<FoundRow> found, IDataContainer container)
    {
        var enumerable = typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(IEnumerable<>)
            ? typeof(T)
            : typeof(T).GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        var itemType = enumerable?.GetGenericArguments()[0];
        if (itemType is null || !itemType.IsGenericType || itemType.GetGenericTypeDefinition().FullName != FindResultTypeName)
            return GenericResult<T>.Failure(
                FileSystemConnectionLog.FactoryValidationFailed(
                    _logger, _connectionName, $"a Find answers a collection of FindResult<T>, and '{typeof(T).Name}' is not one"));

        var records = Records(found, itemType.GetGenericArguments()[0], container);
        if (!records.IsSuccess)
            return records.ToNewResult<T>();

        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType))!;
        for (var i = 0; i < found.Count; i++)
        {
            var result = Activator.CreateInstance(itemType)!;
            itemType.GetProperty("Record")!.SetValue(result, records.Value![i]);
            itemType.GetProperty("MatchedFields")!.SetValue(result, found[i].MatchedFields);
            list.Add(result);
        }

        return list is T typed
            ? GenericResult<T>.Success(typed)
            : GenericResult<T>.Failure(
                FileSystemConnectionLog.FactoryValidationFailed(
                    _logger, _connectionName, $"a Find answer cannot be materialized as '{typeof(T).Name}'"));
    }

    // A record type that a dictionary satisfies (Dictionary, IDictionary, IReadOnlyDictionary, object) is the row itself;
    // any other is built through the shared POCO mapping, as a Query asking for that type is.
    private IGenericResult<IReadOnlyList<object>> Records(IReadOnlyList<FoundRow> found, Type recordType, IDataContainer container)
    {
        if (recordType.IsAssignableFrom(typeof(Dictionary<string, object?>)))
            return GenericResult<IReadOnlyList<object>>.Success(
                found.Select(f => (object)new Dictionary<string, object?>(f.Row)).ToList());

        var materialized = RecordRowMaterializer.MaterializeByType(found.Select(f => f.Row).ToList(), recordType, container, _logger);
        if (!materialized.IsSuccess)
            return materialized.ToNewResult<IReadOnlyList<object>>();

        var objects = materialized.Value!.ToList();
        return objects.Count == found.Count
            ? GenericResult<IReadOnlyList<object>>.Success(objects)
            : GenericResult<IReadOnlyList<object>>.Failure(
                FileSystemConnectionLog.FactoryValidationFailed(
                    _logger, _connectionName, $"{found.Count} matching row(s) materialized as {objects.Count} '{recordType.Name}' record(s)"));
    }
}
