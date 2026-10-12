using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
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
    }

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

        // Why: dispatch explicitly by command type — Query reads, Insert bulk-writes, and the three
        // version-on-write config verbs each map to their own native command. An unrecognized command
        // type FAILS LOUD (NO FALLBACKS) rather than silently falling through to a read — that silent
        // fallthrough was the exact defect this change fixes (a ConfigurationSave/Update/Delete would
        // read stale data and report a false success without writing anything).
        return Task.FromResult(command.CommandType switch
        {
            var t when string.Equals(t, "Query", System.StringComparison.OrdinalIgnoreCase)
                => TranslateRead(command, dataContainer, relativePath),
            var t when string.Equals(t, "Insert", System.StringComparison.OrdinalIgnoreCase)
                => TranslateWrite(command, dataContainer, relativePath),
            var t when string.Equals(t, "ConfigurationSave", System.StringComparison.OrdinalIgnoreCase)
                => TranslateConfigurationSave(command, dataContainer, relativePath),
            var t when string.Equals(t, "Update", System.StringComparison.OrdinalIgnoreCase)
                => TranslateUpdate(command, dataContainer, relativePath),
            var t when string.Equals(t, "ConfigurationRetire", System.StringComparison.OrdinalIgnoreCase)
                => TranslateConfigurationRetire(command, dataContainer, relativePath),
            _ => GenericResult<IFileSystemCommand>.Failure(
                FileSystemConfigurationWriteLog.UnrecognizedCommandType(_logger, _connectionName, command.CommandType)),
        });
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
                queryCommand?.Paging));
    }

    private IGenericResult<IFileSystemCommand> TranslateConfigurationSave(
        IDataCommand command, IDataContainer container, string relativePath)
    {
        // ConfigurationSaveCommand<T> implements IConfigurationSaveCommand (InputData = the record POCO,
        // FieldValues = the owner's key values). A different command carrying this type is a wiring
        // defect — fail loud.
        if (command is not IConfigurationSaveCommand save || save.InputData is null)
        {
            return GenericResult<IFileSystemCommand>.Failure(
                FileSystemConfigurationWriteLog.WriteInputMissing(
                    _logger, _connectionName, container.Name, "ConfigurationSave command carries no record"));
        }

        return GenericResult<IFileSystemCommand>.Success(
            new FileSystemConfigurationSaveCommand(relativePath, container, save.InputData, save.FieldValues, save.ReplacesCurrent));
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

        return GenericResult<IFileSystemCommand>.Success(
            new FileSystemUpdateCommand(relativePath, container, withInput.InputData, filterable.Filter));
    }

    private IGenericResult<IFileSystemCommand> TranslateConfigurationRetire(
        IDataCommand command, IDataContainer container, string relativePath)
    {
        if (command is not IConfigurationRetireCommand retire)
        {
            return GenericResult<IFileSystemCommand>.Failure(
                FileSystemConfigurationWriteLog.WriteInputMissing(
                    _logger, _connectionName, container.Name, "ConfigurationRetire command is not a retire command"));
        }

        // Retiring an owner's rows matches the owner key values the command carries; retiring one row
        // matches the fields of the key the command names, as the container declares them.
        if (retire.FieldValues is { Count: > 0 } fieldValues)
            return GenericResult<IFileSystemCommand>.Success(
                new FileSystemConfigurationDeleteCommand(relativePath, container, fieldValues, retire.MarkDeleted));

        var key = retire.KeyType is null
            ? null
            : container.Keys.FirstOrDefault(k => string.Equals(k.KeyType.Name, retire.KeyType.Name, System.StringComparison.Ordinal));
        if (key is null || key.KeyFields.Count != retire.KeyValues.Count || key.KeyFields.Count == 0)
        {
            return GenericResult<IFileSystemCommand>.Failure(
                FileSystemConfigurationWriteLog.WriteInputMissing(
                    _logger, _connectionName, container.Name,
                    $"ConfigurationRetire names key type '{retire.KeyType?.Name}' with {retire.KeyValues.Count} value(s), which the container does not declare"));
        }

        var match = new Dictionary<string, object?>(System.StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < key.KeyFields.Count; i++)
            match[key.KeyFields[i].LocalField.Name] = retire.KeyValues[i];
        return GenericResult<IFileSystemCommand>.Success(
            new FileSystemConfigurationDeleteCommand(relativePath, container, match, retire.MarkDeleted));
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

        return GenericResult<IFileSystemCommand>.Success(
            new FileSystemWriteCommand(relativePath, container, rowsResult.Value!));
    }

    // Why: the write payload is the schema-agnostic flat name→value row shape (the dictionary write path
    // every record writer supports). Accept a single row, a sequence of rows, or a sequence of DataRecord
    // (projected via ToDictionary()). Anything else is an unsupported payload — fail loud (NO FALLBACKS).
    private IGenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ExtractRows(
        object input, string containerName)
    {
        switch (input)
        {
            case IReadOnlyDictionary<string, object?> single:
                return GenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>.Success(
                    new[] { single });

            case IEnumerable<IReadOnlyDictionary<string, object?>> rows:
                return GenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>.Success(
                    rows.ToList());

            case IEnumerable<DataRecord> records:
                return GenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>.Success(
                    records.Select(r => r.ToDictionary()).ToList());

            default:
                return GenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>.Failure(
                    FileSystemRecordConnectorLog.WriteInputInvalid(
                        _logger, _connectionName, containerName,
                        $"unsupported write payload type '{input.GetType().Name}'"));
        }
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
