using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.SoftDeletes;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using FdwReferenceData.FileSystem.Logging;
using FdwReferenceConnections.FileSystem.Abstractions.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceData.FileSystem;

/// <summary>
/// Translates a universal <see cref="IDataCommand"/> + configured container into the native
/// <see cref="FileSystemRecordCommand"/> the FileSystem connection executes. Read commands (Query)
/// become a read instruction over the container's resolved file path; write commands (Insert) become a
/// write instruction carrying the rows extracted from the command's input data. The relative file path
/// comes from the container's physical <see cref="IStorageContainer.Path"/> (a file path).
/// </summary>
/// <remarks>
/// Why this mirrors <c>HttpProtocolTranslatorAdapter</c>: <c>ConnectionBase</c> calls
/// <c>GetTranslator(commandType).Translate(command, container)</c> to produce the native command before
/// running <c>Execute&lt;T&gt;(nativeCommand, container)</c>. The FileSystem translator is the file-domain
/// equivalent — it does NOT touch the filesystem (that is the connection's job); it only computes the
/// path + operation + payload from the command and container.
/// </remarks>
public sealed class FileSystemCommandTranslator : IDataCommandTranslator<IFileSystemCommand>, IQueryCapability
{
    private readonly string _connectionName;
    private readonly ILogger _logger;
    private readonly Dictionary<string, System.Func<IDataCommand, IDataContainer, string, IGenericResult<IFileSystemCommand>>> _handlers;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemCommandTranslator"/> class.
    /// </summary>
    /// <param name="connectionName">The owning connection name, for structured logging.</param>
    /// <param name="logger">Logger; falls back to <see cref="NullLogger"/> when null.</param>
    public FileSystemCommandTranslator(string connectionName, ILogger? logger)
    {
        _connectionName = connectionName;
        // Why: NullLogger keeps the translator functional without DI logging — the only sanctioned ?? fallback.
        _logger = logger ?? NullLogger.Instance;
        // Why one table: the command types this translator answers are listed once, so Handles and Translate
        // cannot disagree about which a file supports.
        _handlers = new Dictionary<string, System.Func<IDataCommand, IDataContainer, string, IGenericResult<IFileSystemCommand>>>(
            System.StringComparer.OrdinalIgnoreCase)
        {
            ["Query"] = TranslateRead,
            ["Insert"] = TranslateWrite,
            ["Update"] = TranslateUpdate,
            ["Delete"] = TranslateDelete,
            ["Find"] = TranslateFind,
        };
    }

    /// <summary>Gets whether this translator builds a native command for the command type.</summary>
    /// <param name="commandType">The command type name.</param>
    /// <returns>True when <see cref="Translate"/> can build a command for it.</returns>
    public bool Handles(string commandType) => _handlers.ContainsKey(commandType);

    /// <inheritdoc />
    /// <remarks>Derived from this translator's fully qualified type name, like every type option's id.</remarks>
    public int Id { get; } = OptionId.DeriveInt(typeof(FileSystemCommandTranslator).FullName!);

    /// <inheritdoc />
    public string Name => "FileSystem";

    /// <inheritdoc />
    public string Category => "FileSystem";

    /// <inheritdoc />
    public string DomainName => "File";

    /// <inheritdoc />
    public Task<IGenericResult<IFileSystemCommand>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        // Why: the file-record path is keyed on the unified IDataContainer (format + field schema +
        // physical file path), so the translator requires the container be the unified node, not a bare
        // IStorageContainer. A non-IDataContainer here is a wiring defect — fail loud (NO FALLBACKS).
        if (container is not IDataContainer dataContainer)
        {
            return Task.FromResult(
                GenericResult<IFileSystemCommand>.Failure(
                    FileSystemRecordConnectorLog.NotAFilePath(
                        _logger, _connectionName, container.Name, container.GetType().Name)));
        }

        // Why: the relative file path is the container's physical address (IStorageContainer.Path.PathValue)
        // — no gating on Path.Domain here, since it varies by builder (FileSystemDataStoreBuilder sets
        // "File"; the shared GenericDataStoreBuilder that Http also uses sets "Generic") and this
        // translator never inspects it. FileSystemDataStoreBuilder composes PathValue as the FULL
        // relative file path ({DataPath folder}/{container name}{format.CanonicalFileExtension}), so a
        // config header and its typed body under one DataPath resolve to DISTINCT files — this translator
        // reads that value unconditionally and never composes a leaf itself. A missing/empty PathValue
        // still fails loud downstream (FileSystemClient's own root-relative resolution rejects it).
        var relativePath = container.Path.PathValue;

        // Why: dispatch explicitly by command type through the handler table. An unrecognized command type
        // FAILS LOUD (NO FALLBACKS) rather than silently falling through to a read: that silent fallthrough
        // was the defect where a ConfigurationSave/Update/Delete read stale data and reported a false
        // success without writing anything.
        return Task.FromResult(
            _handlers.TryGetValue(command.CommandType, out var handler)
                ? handler(command, dataContainer, relativePath)
                : GenericResult<IFileSystemCommand>.Failure(
                    FileSystemConfigurationWriteLog.UnrecognizedCommandType(_logger, _connectionName, command.CommandType)));
    }

    // Why: a read command that isn't an IQueryCommand carries no filter semantics (e.g. a raw whole-file
    // read) — treat it as "read everything", not a missing-data error. Every config read
    // (Get(name)/Get(id)/List/GetByParentJoin) DOES build an IQueryCommand, so the cast succeeds for the
    // grammar this adapter targets.
    private static IGenericResult<IFileSystemCommand> TranslateRead(
        IDataCommand command, IDataContainer container, string relativePath)
    {
        var queryCommand = command as IQueryCommand;
        return GenericResult<IFileSystemCommand>.Success(
            new FileSystemReadCommand(
                relativePath,
                container,
                queryCommand?.Filter,
                queryCommand?.Joins ?? [],
                // Why carried rather than translated: a file expresses neither, so these travel to
                // the connection to be applied over the decoded rows. Dropping them here is what
                // made a file-backed ORDER BY silently do nothing.
                queryCommand?.Ordering,
                queryCommand?.Paging,
                queryCommand?.ReadsForWrite is true));
    }

    private IGenericResult<IFileSystemCommand> TranslateUpdate(
        IDataCommand command, IDataContainer container, string relativePath)
    {
        // UpdateCommand<T> implements IFilterableCommand (Filter) AND IDataCommandWithInput (the record).
        if (command is not IFilterableCommand filterable
            || command is not IDataCommandWithInput withInput || withInput.InputData is null)
        {
            return GenericResult<IFileSystemCommand>.Failure(
                FileSystemConfigurationWriteLog.WriteInputMissing(
                    _logger, _connectionName, container.Name, "Update command carries no record"));
        }

        var returned = ReturnColumns(command, container);
        if (!returned.IsSuccess)
            return returned.ToNewResult<IFileSystemCommand>();

        return GenericResult<IFileSystemCommand>.Success(
            new FileSystemUpdateCommand(relativePath, container, withInput.InputData, filterable.Filter, returned.Value));
    }

    // The columns an Insert or Update asks to have returned, each resolved to the name the container declares; a column it does
    // not declare is refused, never skipped. None named is a row count.
    private IGenericResult<IReadOnlyList<string>?> ReturnColumns(IDataCommand command, IDataContainer container)
    {
        if (command is not IReturningCommand { ReturnColumns.Count: > 0 } returning)
            return GenericResult<IReadOnlyList<string>?>.Success(null);

        var names = new List<string>(returning.ReturnColumns!.Count);
        foreach (var column in returning.ReturnColumns)
        {
            var field = container.Schema.Fields.FirstOrDefault(f => string.Equals(f.Name, column, System.StringComparison.OrdinalIgnoreCase));
            if (field is null)
                return GenericResult<IReadOnlyList<string>?>.Failure(
                    FileSystemConfigurationWriteLog.ReturnColumnNotDeclared(_logger, _connectionName, container.Name, column));
            names.Add(field.Name);
        }

        return GenericResult<IReadOnlyList<string>?>.Success(names);
    }

    private IGenericResult<IFileSystemCommand> TranslateDelete(
        IDataCommand command, IDataContainer container, string relativePath)
    {
        // DeleteCommand implements IFilterableCommand (Filter); a command of this type without it is a wiring defect.
        // A Delete with no filter is not: it removes every row, as it does on the SQL kinds.
        if (command is not IFilterableCommand filterable)
        {
            return GenericResult<IFileSystemCommand>.Failure(
                FileSystemConfigurationWriteLog.WriteInputMissing(
                    _logger, _connectionName, container.Name, "Delete command is not a filterable command"));
        }

        return GenericResult<IFileSystemCommand>.Success(
            new FileSystemDeleteCommand(relativePath, container, filterable.Filter, (command as ISoftDeletableCommand)?.SoftDelete));
    }

    // FindCommand<T> is generic and lives in a package this project does not reference, so its settings are read by
    // name, as the SQL Find translators read them. A command without a search term is refused: it would match every row.
    private IGenericResult<IFileSystemCommand> TranslateFind(
        IDataCommand command, IDataContainer container, string relativePath)
    {
        var type = command.GetType();
        var searchTerm = type.GetProperty("SearchTerm")?.GetValue(command) as string;
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return GenericResult<IFileSystemCommand>.Failure(
                FileSystemConfigurationWriteLog.FindSearchTermMissing(_logger, _connectionName, container.Name));
        }

        return GenericResult<IFileSystemCommand>.Success(
            new FileSystemFindCommand(
                relativePath,
                container,
                searchTerm,
                type.GetProperty("FieldNames")?.GetValue(command) as IReadOnlyList<string>,
                type.GetProperty("CaseSensitive")?.GetValue(command) is true,
                type.GetProperty("MaxResults")?.GetValue(command) as int?));
    }

    private IGenericResult<IFileSystemCommand> TranslateWrite(
        IDataCommand command,
        IDataContainer container,
        string relativePath)
    {
        if (command is not IDataCommandWithInput withInput || withInput.InputData is null)
        {
            return GenericResult<IFileSystemCommand>.Failure(
                FileSystemRecordConnectorLog.WriteInputInvalid(
                    _logger, _connectionName, container.Name, "command carries no input data"));
        }

        var rowsResult = ExtractRows(withInput.InputData, container.Name);
        if (!rowsResult.IsSuccess)
        {
            return rowsResult.ToNewResult<IFileSystemCommand>();
        }

        var returned = ReturnColumns(command, container);
        if (!returned.IsSuccess)
            return returned.ToNewResult<IFileSystemCommand>();

        return GenericResult<IFileSystemCommand>.Success(
            new FileSystemWriteCommand(relativePath, container, rowsResult.Value!, returned.Value));
    }

    // Why: the write payload is the schema-agnostic flat name→value row shape (the dictionary write path
    // every record writer supports). A row is a read-only or a mutable name→value dictionary: the mutable one is
    // the shape a pipeline's load sends. Accept a single row, a sequence of rows, a sequence of DataRecord
    // (projected via ToDictionary()), or a sequence of typed rows (projected through the row type's POCO mapper,
    // as an Update's record is). Anything else is an unsupported payload — fail loud (NO FALLBACKS).
    private IGenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ExtractRows(
        object input, string containerName)
    {
        switch (input)
        {
            case IReadOnlyDictionary<string, object?> single:
                return GenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>.Success(
                    new[] { single });

            case IDictionary<string, object?> mutableSingle:
                return GenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>.Success(
                    new IReadOnlyDictionary<string, object?>[] { new ReadOnlyDictionary<string, object?>(mutableSingle) });

            case IEnumerable<IReadOnlyDictionary<string, object?>> rows:
                return GenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>.Success(
                    rows.ToList());

            case IEnumerable<DataRecord> records:
                return GenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>.Success(
                    records.Select(r => r.ToDictionary()).ToList());

            case IEnumerable typedRows:
                return ProjectTypedRows(typedRows, containerName);

            default:
                return GenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>.Failure(
                    FileSystemRecordConnectorLog.WriteInputInvalid(
                        _logger, _connectionName, containerName,
                        $"unsupported write payload type '{input.GetType().Name}'"));
        }
    }

    // A typed row becomes its name→value columns through the POCO mapper of its type. A null row or a type with
    // no mapper cannot be projected, so the payload is refused naming the type.
    private IGenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ProjectTypedRows(
        IEnumerable typedRows, string containerName)
    {
        var rows = new List<IReadOnlyDictionary<string, object?>>();
        foreach (var typedRow in typedRows)
        {
            if (typedRow is null)
            {
                return GenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>.Failure(
                    FileSystemRecordConnectorLog.WriteInputInvalid(
                        _logger, _connectionName, containerName, "a row to write is null"));
            }

            if (typedRow is IReadOnlyDictionary<string, object?> flat)
            {
                rows.Add(flat);
                continue;
            }

            if (typedRow is IDictionary<string, object?> mutable)
            {
                rows.Add(new ReadOnlyDictionary<string, object?>(mutable));
                continue;
            }

            var mapper = PocoMapperCollection.ByName(typedRow.GetType().Name);
            if (mapper == PocoMapperCollection.NotFound)
            {
                return GenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>.Failure(
                    FileSystemRecordConnectorLog.WriteInputInvalid(
                        _logger, _connectionName, containerName,
                        $"unsupported write payload type '{typedRow.GetType().Name}'"));
            }

            rows.Add(mapper.MapToParameters(typedRow));
        }

        return GenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>.Success(rows);
    }

    /// <inheritdoc />
    /// <remarks>
    /// A file expresses none of these. There is no WHERE in a CSV, no ORDER BY and no OFFSET — the
    /// rows come out in the order they were written and the connector has to prune, sort and page
    /// them itself. Saying so is the point: the filter reached the native command unchanged before,
    /// so a file-backed query returned everything and nothing reported that.
    /// </remarks>
    public bool CanExpressFilter(IFilterExpression filter) => false;

    /// <inheritdoc />
    public bool CanExpressOrdering(IOrderingExpression ordering) => false;

    /// <inheritdoc />
    public bool CanExpressPaging(IPagingExpression paging) => false;
}
