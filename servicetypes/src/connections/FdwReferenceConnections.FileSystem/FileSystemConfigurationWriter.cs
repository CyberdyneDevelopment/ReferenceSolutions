using FdwReferenceData.FileSystem;
using FdwReferenceData.FileSystem.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.SoftDeletes;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Results;
using Fdw.Services.Connections.RowQuery;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceConnections.FileSystem;

/// <summary>
/// Runs the FileSystem record write verbs: the <c>Insert</c> that adds rows (stamping the next value of the Physical key
/// a row does not carry), the literal in-place <c>Update</c>, and the <c>Delete</c> that removes the rows a filter
/// selects or, with soft-delete options, flags them. Each reads the container's current rows through the
/// <see cref="FileSystemRecordConnector"/>, changes the in-memory row list, and rewrites the WHOLE file.
/// </summary>
/// <remarks>
/// A configuration save or retire is not a verb of its own here: it expands into these (a query, a close or delete,
/// an insert), so a version is written the same way on every kind of connection.
/// <para>
/// Why the per-file lock: every verb is read-whole-file → change in memory → write-whole-file with no
/// coordination between concurrent callers. Two near-simultaneous writes to the SAME file (e.g. <see cref="NextRowId"/>
/// computing <c>max(existing RowId)+1</c> from an in-memory snapshot) can both read the same current-max
/// RowId and both compute the same "next" RowId — the second write silently clobbers the first: one row
/// vanishes, a duplicate RowId remains, and BOTH calls report <c>Success</c>. <see cref="_fileLocks"/>
/// serializes the full read-modify-write cycle per resolved absolute file path so concurrent writers
/// FROM THIS PROCESS never race. See <see cref="_fileLocks"/> for the explicit boundary this does NOT cover.
/// </para>
/// </remarks>
internal sealed class FileSystemConfigurationWriter
{
    // Why: the version columns are a framework-wide convention (every config container declares them). An Update never
    // writes them; a configuration write sets them through the data commands it expands into.
    private const string IsCurrentColumn = "IsCurrent";
    private const string IsDeletedColumn = "IsDeleted";

    // Why: same case-sensitivity convention PathCanonicalizer uses (Linux is case-sensitive, Windows is
    // not) — two relative paths that resolve to the SAME physical file on this OS must share ONE lock.
    private static readonly StringComparer _lockKeyComparer =
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    /// <summary>
    /// Per-absolute-file-path locks serializing the read-modify-write cycle of <see cref="InsertRows"/>/
    /// <see cref="Update"/>/<see cref="DeleteRows"/> against the SAME file.
    /// </summary>
    /// <remarks>
    /// Explicit boundary: this serializes writes to the same file FROM THIS PROCESS only. A second,
    /// different OS process writing the same file at the same instant is NOT guarded here — that would
    /// need real OS-level file locking or optimistic concurrency (e.g. an ETag/version check), and is out
    /// of scope for this pass: the configuration domain is low-write-frequency and has no current
    /// multi-process writer (a single app process owns the ConfigurationGateway connection).
    /// </remarks>
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocks = new(_lockKeyComparer);

    private readonly FileSystemRecordConnector _connector;
    private readonly string _connectionName;
    private readonly string _root;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemConfigurationWriter"/> class.
    /// </summary>
    /// <param name="connector">The record connector that performs the config-driven file read/write.</param>
    /// <param name="connectionName">The owning connection name, for structured logging.</param>
    /// <param name="root">The connection's canonicalized Root — used to resolve the absolute file path each verb locks on.</param>
    /// <param name="logger">Logger; falls back to <see cref="NullLogger"/> when null.</param>
    public FileSystemConfigurationWriter(FileSystemRecordConnector connector, string connectionName, string root, ILogger? logger)
    {
        _connector = connector;
        _connectionName = connectionName;
        _root = root;
        // Why: NullLogger keeps the writer functional without DI logging — the only sanctioned ?? fallback.
        _logger = logger ?? NullLogger.Instance;
    }

    // Why: resolves the SAME absolute path the actual I/O will use (PathCanonicalizer.Resolve is pure
    // string manipulation, no I/O) — the lock key must match the physical file the verb is about to touch.
    private IGenericResult<string> ResolveAbsolutePath(string relativePath)
        => PathCanonicalizer.Resolve(_root, relativePath, _connectionName, _logger);

    private static SemaphoreSlim GetFileLock(string absolutePath) =>
        _fileLocks.GetOrAdd(absolutePath, static _ => new SemaphoreSlim(1, 1));

    // ════════════════════════════════════════════════════════════════════════════
    // Update — literal, in-place mutation (NOT version-on-write)
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Mutates the matched rows' non-key columns in place (same RowId, version flags untouched) and
    /// rewrites the whole file. Matches on the command's filter; refuses to run without one.
    /// </summary>
    public Task<IGenericResult<int>> Update(
        FileSystemUpdateCommand command, CancellationToken cancellationToken = default)
        => UpdateLocked(command, null, cancellationToken);

    /// <summary>Applies an update as <see cref="Update"/> does and answers the rows it modified, as they are now written.</summary>
    /// <param name="command">The update command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The modified rows; or the failure that stopped it.</returns>
    public async Task<IGenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>> UpdateReturning(
        FileSystemUpdateCommand command, CancellationToken cancellationToken = default)
    {
        var modified = new List<IReadOnlyDictionary<string, object?>>();
        var result = await UpdateLocked(command, modified, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess
            ? GenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>.Success(modified)
            : result.ToNewResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>();
    }

    private async Task<IGenericResult<int>> UpdateLocked(
        FileSystemUpdateCommand command, List<IReadOnlyDictionary<string, object?>>? sink, CancellationToken cancellationToken)
    {
        var pathResult = ResolveAbsolutePath(command.RelativePath);
        if (!pathResult.IsSuccess)
            return pathResult.ToNewResult<int>();

        var fileLock = GetFileLock(pathResult.Value!);
        await fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await UpdateCore(command, sink, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            fileLock.Release();
        }
    }

    private async Task<IGenericResult<int>> UpdateCore(
        FileSystemUpdateCommand command, List<IReadOnlyDictionary<string, object?>>? sink, CancellationToken cancellationToken)
    {
        var container = command.Container;
        FileSystemConfigurationWriteLog.UpdateStarting(_logger, _connectionName, container.Name);

        // Why: the config Update always carries a WHERE (Id = record.Id). No filter means no way to identify
        // rows — refuse rather than silently mutate every row (NO FALLBACKS), unlike a bare SQL UPDATE.
        if (command.Filter?.Root is null)
            return GenericResult<int>.Failure(
                FileSystemConfigurationWriteLog.UpdateFilterMissing(_logger, _connectionName, container.Name));

        var filterColumns = RecordColumnValidator.ValidateFilterColumns(command.Filter.Root, container, null, null, _logger);
        if (!filterColumns.IsSuccess)
            return filterColumns.ToNewResult<int>();

        var mapper = PocoMapperCollection.ByName(command.Record.GetType().Name);
        if (mapper == PocoMapperCollection.NotFound)
            return GenericResult<int>.Failure(
                FileSystemConfigurationWriteLog.PocoMapperNotFound(_logger, _connectionName, command.Record.GetType().Name));

        var parameters = new Dictionary<string, object?>(mapper.MapToParameters(command.Record), StringComparer.OrdinalIgnoreCase);
        var setColumns = UpdatableColumns(container, parameters);
        if (setColumns.Count == 0)
            return GenericResult<int>.Failure(
                FileSystemConfigurationWriteLog.NoUpdatableColumns(
                    _logger, _connectionName, container.Name, command.Record.GetType().Name));

        var rowsResult = await _connector
            .ReadExistingOrEmpty(container, command.RelativePath, cancellationToken).ConfigureAwait(false);
        if (!rowsResult.IsSuccess)
            return rowsResult.ToNewResult<int>();

        var rows = rowsResult.Value!.Select(CopyRow).ToList();
        var modified = new List<IReadOnlyDictionary<string, object?>>();
        foreach (var row in rows)
        {
            if (!RecordRowMatcher.Matches(row, null, null, command.Filter.Root))
                continue;
            foreach (var column in setColumns)
                row[column] = parameters.TryGetValue(column, out var value) ? value : null;
            modified.Add(row);
        }

        if (modified.Count == 0)
        {
            FileSystemConfigurationWriteLog.UpdateCompleted(_logger, _connectionName, container.Name, 0);
            return GenericResult<int>.Success(0);
        }

        var validate = RecordRowValidator.Validate(modified, container, _logger);
        if (!validate.IsSuccess)
            return validate.ToNewResult<int>();

        var writeResult = await WriteRows(container, command.RelativePath, rows, cancellationToken).ConfigureAwait(false);
        if (!writeResult.IsSuccess)
            return writeResult;

        sink?.AddRange(modified);
        FileSystemConfigurationWriteLog.UpdateCompleted(_logger, _connectionName, container.Name, modified.Count);
        return GenericResult<int>.Success(modified.Count);
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Delete — removes the rows a filter selects, or every row when there is no filter
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Removes the rows the command's filter selects from the container's file, and every row when the command has no
    /// filter (a Delete with no restrictions empties the container, as it does on the SQL kinds).
    /// </summary>
    /// <param name="command">The delete command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of rows removed; or the failure that stopped it.</returns>
    public async Task<IGenericResult<int>> DeleteRows(
        FileSystemDeleteCommand command, CancellationToken cancellationToken = default)
    {
        var pathResult = ResolveAbsolutePath(command.RelativePath);
        if (!pathResult.IsSuccess)
            return pathResult.ToNewResult<int>();

        var fileLock = GetFileLock(pathResult.Value!);
        await fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await DeleteRowsCore(command, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            fileLock.Release();
        }
    }

    private async Task<IGenericResult<int>> DeleteRowsCore(
        FileSystemDeleteCommand command, CancellationToken cancellationToken)
    {
        var container = command.Container;
        FileSystemConfigurationWriteLog.DeleteRowsStarting(_logger, _connectionName, container.Name);

        if (command.Filter?.Root is { } root)
        {
            var filterColumns = RecordColumnValidator.ValidateFilterColumns(root, container, null, null, _logger);
            if (!filterColumns.IsSuccess)
                return filterColumns.ToNewResult<int>();
        }

        if (command.SoftDelete is { } soft)
            return await SoftDeleteRowsCore(command, soft, cancellationToken).ConfigureAwait(false);

        var rowsResult = await _connector
            .ReadExistingOrEmpty(container, command.RelativePath, cancellationToken).ConfigureAwait(false);
        if (!rowsResult.IsSuccess)
            return rowsResult.ToNewResult<int>();

        var kept = rowsResult.Value!
            .Where(row => command.Filter?.Root is { } selector && !RecordRowMatcher.Matches(row, null, null, selector))
            .Select(CopyRow)
            .ToList();
        var removed = rowsResult.Value!.Count - kept.Count;
        if (removed == 0)
        {
            FileSystemConfigurationWriteLog.DeleteRowsCompleted(_logger, _connectionName, container.Name, 0);
            return GenericResult<int>.Success(0);
        }

        var writeResult = await WriteRows(container, command.RelativePath, kept, cancellationToken).ConfigureAwait(false);
        if (!writeResult.IsSuccess)
            return writeResult;

        FileSystemConfigurationWriteLog.DeleteRowsCompleted(_logger, _connectionName, container.Name, removed);
        return GenericResult<int>.Success(removed);
    }

    // Flags the rows the soft delete selects and keeps every row in the file; answers the number flagged.
    private async Task<IGenericResult<int>> SoftDeleteRowsCore(
        FileSystemDeleteCommand command, SoftDeleteOptions soft, CancellationToken cancellationToken)
    {
        var container = command.Container;
        var problem = SoftDeleteProblem(soft, container);
        if (problem is not null)
            return GenericResult<int>.Failure(
                FileSystemConfigurationWriteLog.SoftDeleteRefused(_logger, _connectionName, container.Name, problem));

        var rowsResult = await _connector
            .ReadExistingOrEmpty(container, command.RelativePath, cancellationToken).ConfigureAwait(false);
        if (!rowsResult.IsSuccess)
            return rowsResult.ToNewResult<int>();

        var rows = rowsResult.Value!.Select(CopyRow).ToList();
        var flagged = FlagRows(rows, soft, command.Filter);
        if (flagged > 0)
        {
            var writeResult = await WriteRows(container, command.RelativePath, rows, cancellationToken).ConfigureAwait(false);
            if (!writeResult.IsSuccess)
                return writeResult;
        }

        FileSystemConfigurationWriteLog.SoftDeleteRowsCompleted(_logger, _connectionName, container.Name, flagged);
        return GenericResult<int>.Success(flagged);
    }

    // Why: the same checks the SQL kinds make. The options must be consistent and every column they name must be one
    // the container declares, so a typo fails by name instead of adding a column to the file.
    private static string? SoftDeleteProblem(SoftDeleteOptions soft, IDataContainer container)
    {
        var problem = soft.FirstProblem();
        if (problem is not null)
            return problem.Message;

        var names = new List<string> { soft.DeletedFlagColumn };
        if (soft.DeletedAtColumn is not null)
            names.Add(soft.DeletedAtColumn);
        if (soft.Marker is not null)
            names.Add(soft.Marker.Column);
        if (soft.AbsentFromSource is not null)
            names.AddRange(soft.AbsentFromSource.KeyColumns);

        var declared = container.Schema.Fields;
        var unknown = names.FirstOrDefault(n => !declared.Any(f => string.Equals(f.Name, n, StringComparison.OrdinalIgnoreCase)));
        return unknown is null ? null : $"container {container.Name} does not declare column '{unknown}'";
    }

    private static int FlagRows(IReadOnlyList<Dictionary<string, object?>> rows, SoftDeleteOptions soft, IFilterExpression? filter)
    {
        var flagged = 0;
        foreach (var row in rows)
        {
            if (!SoftDeleteSelects(row, soft, filter))
                continue;
            row[soft.DeletedFlagColumn] = soft.FlagValue;
            if (soft.DeletedAtColumn is not null)
                row[soft.DeletedAtColumn] = DateTimeOffset.UtcNow;
            flagged++;
        }

        return flagged;
    }

    // A row is selected when it does not already hold the flag value (an empty flag cell does not), the filter matches
    // it, its marker column holds the marker value, and its keys are absent from the source set when the options carry one.
    private static bool SoftDeleteSelects(Dictionary<string, object?> row, SoftDeleteOptions soft, IFilterExpression? filter)
    {
        var flag = GetCell(row, soft.DeletedFlagColumn);
        if (flag is not null && IsFlagTrue(flag) == soft.FlagValue)
            return false;
        if (filter?.Root is { } selector && !RecordRowMatcher.Matches(row, null, null, selector))
            return false;
        if (soft.Marker is { } marker && !KeyValuesEqual(GetCell(row, marker.Column), marker.Value))
            return false;
        return soft.AbsentFromSource is not { } source
            || !source.Rows.Any(present => source.KeyColumns.All(k => present.TryGetValue(k, out var value) && KeyValuesEqual(GetCell(row, k), value)));
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Insert — adds rows to the ones the file holds
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Adds the command's rows to the rows the file already holds (an absent file holds none) and rewrites the
    /// whole file, under the same per-file lock as the other writes. A row that carries no Physical key value is given the
    /// next one. A file keeps its rows across inserts, as every other connection kind does; a load that wants a fresh file
    /// empties it first with a Delete that has no filter.
    /// </summary>
    /// <param name="command">The insert command carrying the rows.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of rows added; or the failure that stopped it.</returns>
    public Task<IGenericResult<int>> InsertRows(
        FileSystemWriteCommand command, CancellationToken cancellationToken = default)
        => InsertRowsLocked(command, null, cancellationToken);

    /// <summary>Adds rows as <see cref="InsertRows"/> does and answers the rows it added, as they are now written.</summary>
    /// <param name="command">The insert command carrying the rows.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added rows; or the failure that stopped it.</returns>
    public async Task<IGenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>> InsertRowsReturning(
        FileSystemWriteCommand command, CancellationToken cancellationToken = default)
    {
        var added = new List<IReadOnlyDictionary<string, object?>>();
        var result = await InsertRowsLocked(command, added, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess
            ? GenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>.Success(added)
            : result.ToNewResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>();
    }

    private async Task<IGenericResult<int>> InsertRowsLocked(
        FileSystemWriteCommand command, List<IReadOnlyDictionary<string, object?>>? sink, CancellationToken cancellationToken)
    {
        var pathResult = ResolveAbsolutePath(command.RelativePath);
        if (!pathResult.IsSuccess)
            return pathResult.ToNewResult<int>();

        var fileLock = GetFileLock(pathResult.Value!);
        await fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await InsertRowsCore(command, sink, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            fileLock.Release();
        }
    }

    private async Task<IGenericResult<int>> InsertRowsCore(
        FileSystemWriteCommand command, List<IReadOnlyDictionary<string, object?>>? sink, CancellationToken cancellationToken)
    {
        var container = command.Container;
        FileSystemConfigurationWriteLog.InsertRowsStarting(_logger, _connectionName, container.Name);

        var rowsResult = await _connector
            .ReadExistingOrEmpty(container, command.RelativePath, cancellationToken).ConfigureAwait(false);
        if (!rowsResult.IsSuccess)
            return rowsResult.ToNewResult<int>();

        // Why: a file has no database to assign the Physical key (RowId, an identity on a SQL kind), so a row that does not
        // carry one is given the next value here, after the rows already in the file and the rows added before it.
        var all = rowsResult.Value!.Select(CopyRow).ToList();
        var physicalKey = PhysicalKeyColumn(container);
        var added = new List<Dictionary<string, object?>>(command.Rows.Count);
        foreach (var source in command.Rows)
        {
            var row = CopyRow(source);
            if (physicalKey is not null && GetCell(row, physicalKey) is null)
                row[physicalKey] = NextRowId(all, physicalKey);
            all.Add(row);
            added.Add(row);
        }

        var writeResult = await WriteRows(container, command.RelativePath, all, cancellationToken).ConfigureAwait(false);
        if (!writeResult.IsSuccess)
            return writeResult;

        sink?.AddRange(added);
        FileSystemConfigurationWriteLog.InsertRowsCompleted(
            _logger, _connectionName, container.Name, command.Rows.Count, rowsResult.Value!.Count);
        return GenericResult<int>.Success(command.Rows.Count);
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Shared helpers
    // ════════════════════════════════════════════════════════════════════════════

    private Task<IGenericResult<int>> WriteRows(
        IDataContainer container,
        string relativePath,
        IReadOnlyList<Dictionary<string, object?>> rows,
        CancellationToken cancellationToken)
        => _connector.Write(
            container,
            relativePath,
            rows.Cast<IReadOnlyDictionary<string, object?>>().ToList(),
            cancellationToken);

    // Why: the SET column list for an Update — every declared field the record supplies a value for,
    // excluding the physical PK and the version flags (mirrors MsSqlUpdateTranslator's exclusions; the
    // version flags are excluded by name here because FileSystem field metadata carries no IsSystemProvided).
    private static List<string> UpdatableColumns(IDataContainer container, Dictionary<string, object?> parameters)
    {
        var physicalKeyColumn = PhysicalKeyColumn(container);
        var columns = new List<string>();
        var nodes = container.Nodes;
        for (var i = 0; i < nodes.Count; i++)
        {
            var name = nodes[i].Name;
            if (physicalKeyColumn is not null && string.Equals(name, physicalKeyColumn, StringComparison.OrdinalIgnoreCase))
                continue;
            if (string.Equals(name, IsCurrentColumn, StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, IsDeletedColumn, StringComparison.OrdinalIgnoreCase))
                continue;
            if (parameters.ContainsKey(name))
                columns.Add(name);
        }
        return columns;
    }

    // Why: the physical version PK column (RowId) — the Physical key's field. Version RowId assignment and
    // FK resolution both key off this.
    private static string? PhysicalKeyColumn(IDataContainer container)
    {
        var keys = container.Keys;
        for (var i = 0; i < keys.Count; i++)
        {
            var key = keys[i];
            if (string.Equals(key.KeyType.Name, "Physical", StringComparison.Ordinal) && key.KeyFields.Count > 0)
                return key.KeyFields[0].LocalField.Name;
        }
        return null;
    }

    private static long NextRowId(IReadOnlyList<Dictionary<string, object?>> rows, string physicalKeyColumn)
    {
        long max = 0;
        foreach (var row in rows)
        {
            var value = ToLong(GetCell(row, physicalKeyColumn));
            if (value is not null && value.Value > max)
                max = value.Value;
        }
        return max + 1;
    }

    private static Dictionary<string, object?> CopyRow(IReadOnlyDictionary<string, object?> row)
        => new(row, StringComparer.OrdinalIgnoreCase);

    private static object? GetCell(Dictionary<string, object?> row, string column)
        => row.TryGetValue(column, out var value) ? value : null;

    // Why: version/FK keys arrive from a decoded row as raw format primitives (a Guid column reads back as
    // a string, a RowId column as a long) while the write-side value is a native Guid/long — compare by
    // coercing to a common representation rather than requiring identical CLR types. Same tolerance the
    // shared RecordRowMatcher applies to filter values.
    private static bool KeyValuesEqual(object? left, object? right)
    {
        if (left is null || right is null)
            return false;

        var leftGuid = ToGuid(left);
        var rightGuid = ToGuid(right);
        if (leftGuid is not null && rightGuid is not null)
            return leftGuid.Value == rightGuid.Value;

        var leftLong = ToLong(left);
        var rightLong = ToLong(right);
        if (leftLong is not null && rightLong is not null)
            return leftLong.Value == rightLong.Value;

        return string.Equals(
            Convert.ToString(left, CultureInfo.InvariantCulture),
            Convert.ToString(right, CultureInfo.InvariantCulture),
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsFlagTrue(object? value) => value switch
    {
        null => false,
        bool flag => flag,
        string text when bool.TryParse(text, out var parsed) => parsed,
        string text when int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) => number != 0,
        long integral => integral != 0,
        int integer => integer != 0,
        _ => false,
    };

    private static Guid? ToGuid(object value) => value switch
    {
        Guid guid => guid,
        string text when Guid.TryParse(text, out var parsed) => parsed,
        _ => null,
    };

    private static long? ToLong(object? value) => value switch
    {
        null => null,
        long integral => integral,
        int integer => integer,
        short small => small,
        byte b => b,
        string text when long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
        _ => null,
    };
}
