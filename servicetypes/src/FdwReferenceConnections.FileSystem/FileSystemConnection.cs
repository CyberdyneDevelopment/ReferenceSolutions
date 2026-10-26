using FdwReferenceData.FileSystem;
using FdwReferenceData.FileSystem.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Documents.Abstractions;
using Fdw.Documents.Abstractions.Capabilities;
using Fdw.Documents.Abstractions.Commands;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using FdwReferenceConnections.FileSystem.Abstractions;
using FdwReferenceConnections.FileSystem.Abstractions.Logging;
using Fdw.Services.Connections.Logging;
using Fdw.Services.Connections.RowQuery;
using Microsoft.Extensions.Logging;

namespace FdwReferenceConnections.FileSystem;

/// <summary>
/// FileSystem connection implementation.
/// Wraps a <see cref="FileSystemClient"/> and enforces Root path isolation.
/// </summary>
/// <remarks>
/// Inherits <c>ConnectionBase&lt;IFileSystemCommand, ...&gt;</c> so the DataGateway-routed
/// command path remains wirable in 1.2.0 if the canary experiment succeeds.
/// No concrete <c>IFileSystemCommand</c> types ship in 1.1.1 — connectors call
/// <see cref="Client"/> directly per the §1.1 canary experiment.
/// </remarks>
public sealed class FileSystemConnection
    : ConnectionBase<IFileSystemCommand, FileSystemConnectionConfiguration, FileSystemConnection>,
      IFileSystemConnection, ISupportsHealthProbe, ITransactionalDataConnection, IDocumentConnection
{
    private readonly FileSystemClient _client;
    private readonly FileSystemCommandTranslator _translator;
    private readonly FileSystemTranslatorNotFound _translatorNotFound;
    private readonly FileSystemRecordConnector _recordConnector;
    private readonly FileSystemConfigurationWriter _configurationWriter;
    private readonly FileSystemFinder _finder;
    private readonly FileSystemDocumentStore _documents;

    /// <inheritdoc />
    public string Root { get; }

    /// <inheritdoc />
    public IFileSystemClient Client => _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemConnection"/> class.
    /// </summary>
    /// <param name="configuration">The validated FileSystem connection configuration.</param>
    /// <param name="logger">Logger instance; falls back to NullLogger if null.</param>
    public FileSystemConnection(
        FileSystemConnectionConfiguration configuration,
        ILogger<FileSystemConnection>? logger)
        : base(logger, configuration)
    {
        // Why: Root is canonicalized once at construction. All relative paths from the client
        // are validated against this canonicalized root before any I/O occurs.
        Root = Path.GetFullPath(configuration.Root);
        _client = new FileSystemClient(Root, Name, logger);
        // Why: the translator computes path + operation from the command/container (no I/O); the record
        // connector runs the config-driven record source/writer over the file client. Both share the
        // connection name + logger so structured logging traces back to this connection.
        _translator = new FileSystemCommandTranslator(Name, logger);
        _translatorNotFound = new FileSystemTranslatorNotFound(Name, logger);
        _recordConnector = new FileSystemRecordConnector(_client, Name, logger);
        // Why: the config write verbs (ConfigurationSave/Update/ConfigurationDelete) read the container's
        // current rows through the record connector, mutate them per version-on-write semantics, and rewrite
        // the whole file — this writer owns that read-modify-write, keeping Execute a thin dispatcher.
        _configurationWriter = new FileSystemConfigurationWriter(_recordConnector, Name, Root, logger);
        _finder = new FileSystemFinder(Name, Logger);
        // Why: ConnectionBase already stored the NullLogger-fallbacked logger in Logger; use it so the
        // message actually emits (a message built on a throwaway NullLogger never logs).
        _documents = new FileSystemDocumentStore(Root, Name, logger);
        FileSystemConnectionLog.Created(Logger, Name, Root);
    }

    /// <summary>
    /// Gets the scope of the documents: the whole root folder. A narrower scope is applied by whoever uses the connection (FDW-1581).
    /// </summary>
    public DocumentScope Scope { get; } = new(DocumentPath.Root);

    /// <summary>
    /// Gets what the connection can do with documents: create-only writes, ranged and streamed reads, copy and move. It keeps no
    /// metadata, no versions and no content hash, and creates no read links (FDW-1798).
    /// </summary>
    public IReadOnlyCollection<IDocumentCapability> Capabilities { get; } =
    [
        DocumentCapabilities.ByName("CreateOnlyWrites"),
        DocumentCapabilities.ByName("RangedReads"),
        DocumentCapabilities.ByName("StreamedReads"),
        DocumentCapabilities.ByName("Copy"),
        DocumentCapabilities.ByName("Move"),
    ];

    /// <inheritdoc />
    public Task<IGenericResult<T>> Execute<T>(IDocumentCommand<T> command, CancellationToken cancellationToken = default)
        => _documents.Execute(command, cancellationToken);

    // Set while a command runs through a unit of work (the unit's Execute sets it), so a command that assembles its own result
    // there runs its steps in that unit and does not try to open another, which the client refuses while one is open.
    private static readonly AsyncLocal<bool> _inUnit = new();

    // The units a client holds cannot overlap, so a command that assembles its own result takes its turn: two such commands
    // running at once on this connection (two saves of one file) run one after the other, each in its own unit.
    private readonly SemaphoreSlim _assembledGate = new(1, 1);

    /// <summary>
    /// Gets a value indicating whether the command being run came through a unit of work, so it runs inside that unit and its
    /// undo log.
    /// </summary>
    protected override bool InUnitOfWork => _inUnit.Value;

    /// <inheritdoc />
    protected override async Task<IGenericResult<IDataConnectionTransaction>> BeginAssembledUnit(
        ITransactionalDataConnection transactional, CancellationToken cancellationToken)
    {
        await _assembledGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        var begun = await transactional.BeginTransaction(cancellationToken).ConfigureAwait(false);
        if (begun.IsFailure)
        {
            _assembledGate.Release();
            return begun;
        }

        return GenericResult<IDataConnectionTransaction>.Success(new GatedFileSystemUnit(begun.Value!, _assembledGate));
    }

    // Runs a command for a unit of work, marking it as inside one for as long as it runs.
    internal async Task<IGenericResult<T>> ExecuteInUnit<T>(IDataCommand command, IDataContainer container, CancellationToken cancellationToken)
    {
        var outer = _inUnit.Value;
        _inUnit.Value = true;
        try
        {
            return await Execute<T>(command, container, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _inUnit.Value = outer;
        }
    }

    /// <inheritdoc />
    protected override IDataCommandTranslator<IFileSystemCommand> GetTranslator(string commandType)
    {
        // Why: the FileSystem domain has one translator for the command types a file supports (it maps the command
        // to a read/write file operation). ConnectionBase treats the "NotFound" sentinel as "no translator", and
        // for a command that declares an expansion (Upsert, BulkUpsert, BulkUpdate) it then runs the simpler
        // commands the file does answer, so a command type the translator does not handle must be reported
        // missing rather than failing inside the translator.
        return _translator.Handles(commandType) ? _translator : _translatorNotFound;
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<T>> Execute<T>(
        IFileSystemCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken)
    {
        // Why: the native command is always a read/write FileSystemRecordCommand built by the translator;
        // dispatch by the command type (no direction flag), and fail loud on any other IFileSystemCommand
        // (a wiring defect — NO FALLBACKS).
        return command switch
        {
            FileSystemWriteCommand write => await ExecuteInsert<T>(write, cancellationToken).ConfigureAwait(false),
            FileSystemReadCommand read => await ExecuteRead<T>(read, cancellationToken).ConfigureAwait(false),
            FileSystemFindCommand find => await ExecuteFind<T>(find, cancellationToken).ConfigureAwait(false),
            FileSystemUpdateCommand update =>
                WrapCount<T>(await _configurationWriter.Update(update, cancellationToken).ConfigureAwait(false)),
            FileSystemDeleteCommand delete =>
                WrapCount<T>(await _configurationWriter.DeleteRows(delete, cancellationToken).ConfigureAwait(false)),
            _ => GenericResult<T>.Failure(
                FileSystemConnectionLog.FactoryValidationFailed(
                    Logger,
                    Name,
                    $"unsupported native command '{command.GetType().Name}'"))
        };
    }

    // Why: the config write verbs return an affected-row count (int). Materialize it into T when T can hold
    // it (int/long/bool/object); for any other T — e.g. the provider's Execute<TConfig> header save, which
    // checks IsSuccess and returns its own record — the write produced no typed value, so return
    // Success(default). The write already succeeded; this NEVER fails on materialization. Mirrors
    // MsSqlConnection.ConvertScalarResult, so the version-on-write save behaves identically across transports.
    private static IGenericResult<T> WrapCount<T>(IGenericResult<int> writeResult)
    {
        if (!writeResult.IsSuccess)
            return writeResult.ToNewResult<T>();

        var count = writeResult.Value;
        var targetType = typeof(T);

        if (targetType == typeof(int))
            return GenericResult<T>.Success((T)(object)count);
        if (targetType == typeof(long))
            return GenericResult<T>.Success((T)(object)(long)count);
        if (targetType == typeof(bool))
            return GenericResult<T>.Success((T)(object)(count > 0));
        if (targetType == typeof(object))
            return GenericResult<T>.Success((T)(object)count);

        return GenericResult<T>.Success(default!);
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult> Execute(
        IFileSystemCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken)
    {
        var result = await Execute<object>(command, container, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? GenericResult.Success() : result;
    }

    private async Task<IGenericResult<T>> ExecuteRead<T>(
        FileSystemReadCommand command,
        CancellationToken cancellationToken)
    {
        var matchResult = await EvaluateRows(command, cancellationToken).ConfigureAwait(false);
        if (!matchResult.IsSuccess)
        {
            return matchResult.ToNewResult<T>();
        }

        // Why: the no-DTO read yields rows as flat name→value maps. Materialize into the requested T when
        // it is the rows collection itself; otherwise materialize through the shared PocoMapper-based
        // path (F6 Stage 1 typed-T materialization).
        if (matchResult.Value is T typed)
        {
            return GenericResult<T>.Success(typed);
        }

        if (typeof(T) == typeof(IEnumerable<IReadOnlyDictionary<string, object?>>))
        {
            return GenericResult<T>.Success((T)(object)matchResult.Value!);
        }

        return RecordRowMaterializer.Materialize<T>(matchResult.Value!, command.Container, Logger);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Why this override exists: the base implementation fails loud for every transport that doesn't
    /// override it (see its own remarks) -- FileSystem didn't, so a typed-list cascade child (e.g.
    /// CorsConfiguration.Origins reading hst/CorsOrigin.json) always failed with a bare "Execution
    /// failed" and no detail, for every FileSystem-backed store, regardless of what the JSON
    /// contained. Reuses the exact read+evaluate step <see cref="ExecuteRead{T}"/> uses -- a join
    /// against the owner container behaves identically whether the caller wants a generic T or a
    /// runtime elementType.
    /// </remarks>
    protected override async Task<IGenericResult<IEnumerable<object>>> ExecuteRowsByType(
        IFileSystemCommand command,
        IStorageContainer container,
        Type elementType,
        CancellationToken cancellationToken)
    {
        // A write that names columns to return answers the rows it wrote, projected to those columns.
        if (command is FileSystemWriteCommand { ReturnColumns: { Count: > 0 } insertColumns } insert)
            return await ReturnWritten(
                await _configurationWriter.InsertRowsReturning(insert, cancellationToken).ConfigureAwait(false), insertColumns, elementType).ConfigureAwait(false);
        if (command is FileSystemUpdateCommand { ReturnColumns: { Count: > 0 } updateColumns } update)
            return await ReturnWritten(
                await _configurationWriter.UpdateReturning(update, cancellationToken).ConfigureAwait(false), updateColumns, elementType).ConfigureAwait(false);

        if (command is not FileSystemReadCommand read)
        {
            return GenericResult<IEnumerable<object>>.Failure(
                FileSystemConnectionLog.FactoryValidationFailed(
                    Logger,
                    Name,
                    $"unsupported native command '{command.GetType().Name}' for row-by-type execution"));
        }

        var matchResult = await EvaluateRows(read, cancellationToken).ConfigureAwait(false);
        if (!matchResult.IsSuccess)
        {
            return matchResult.ToNewResult<IEnumerable<object>>();
        }

        // A caller asking for the raw Dictionary<string, object?> shape (e.g. ImplementationProviderBase's
        // owner-physical-key capture) wants the matched rows themselves, not a POCO -- a dictionary has no
        // generated mapper to look up, and routing it through MaterializeByType always fails with "No
        // PocoMapper registered for type 'Dictionary`2'". Every row this connector produces is already an
        // IReadOnlyDictionary<string, object?>, so satisfying that request is a copy, not a lookup.
        if (elementType == typeof(Dictionary<string, object?>))
        {
            return GenericResult<IEnumerable<object>>.Success(
                matchResult.Value!.Select(row => (object)new Dictionary<string, object?>(row)).ToList());
        }

        return RecordRowMaterializer.MaterializeByType(matchResult.Value!, elementType, read.Container, Logger);
    }

    // The columns a write was asked to return, taken from the rows it wrote. A file generates no column of its own, so a column the
    // row does not carry is returned as null. Only dictionary rows are answered.
    private Task<IGenericResult<IEnumerable<object>>> ReturnWritten(
        IGenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>> written, IReadOnlyList<string> columns, Type elementType)
    {
        if (!written.IsSuccess)
            return Task.FromResult(written.ToNewResult<IEnumerable<object>>());
        if (elementType != typeof(Dictionary<string, object?>))
            return Task.FromResult(GenericResult<IEnumerable<object>>.Failure(
                FileSystemConnectionLog.FactoryValidationFailed(
                    Logger, Name, $"returned columns are answered as dictionary rows, and '{elementType.Name}' was asked for")));

        return Task.FromResult(GenericResult<IEnumerable<object>>.Success(
            written.Value!
                .Select(row => (object)columns.ToDictionary(
                    column => column,
                    column => row.TryGetValue(column, out var value) ? value : null,
                    StringComparer.OrdinalIgnoreCase))
                .ToList()));
    }

    private async Task<IGenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>> EvaluateRows(
        FileSystemReadCommand command,
        CancellationToken cancellationToken)
    {
        // Why: a file is created by its first write, so rows read to be written over come from a file that may not exist yet
        // and hold none; a read for any other purpose of a file that is not there is refused.
        var readResult = command.MissingFileHoldsNoRows
            ? await _recordConnector.ReadExistingOrEmpty(command.Container, command.RelativePath, cancellationToken).ConfigureAwait(false)
            : await _recordConnector.Read(command.Container, command.RelativePath, cancellationToken).ConfigureAwait(false);
        if (!readResult.IsSuccess)
        {
            return readResult.ToNewResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>();
        }

        // The connector applies whatever the translator declined, which is what IQueryCapability
        // exists to say: "Whatever it declines is the connector's job, applied over the record stream
        // before the rows leave." This asks rather than assuming. Today the answer is the same for all
        // three - a file has no WHERE, no ORDER BY and no OFFSET - so behaviour is unchanged; what
        // changes is that the declaration is now consulted instead of duplicated as an assumption
        // here, and a translator that later learns to express one of them stops being compensated for
        // it twice.
        //
        // Asked on the concrete translator rather than through IQueryCapability: this connection owns
        // exactly one translator and it implements the interface, so a cast would be a test whose
        // answer is known at compile time. The "a translator that declares nothing expresses nothing"
        // reading belongs wherever a connection can hold a translator that may not implement it, not
        // here.
        var filter = command.Filter is { } f && _translator.CanExpressFilter(f) ? null : command.Filter;
        var ordering = command.Ordering is { } o && _translator.CanExpressOrdering(o) ? null : command.Ordering;
        var paging = command.Paging is { } p && _translator.CanExpressPaging(p) ? null : command.Paging;

        // The evaluator orders before it pages, so Paging selects from sorted rows.
        return await RecordQueryEvaluator.Evaluate(
            readResult.Value!,
            command.Container,
            filter,
            command.Joins,
            (joinedContainerName, ct) => LoadJoinedRows(command.Container, joinedContainerName, ct),
            Logger,
            ordering,
            paging,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    // Why: resolves the JOIN target container from the primary container's tree-navigation parent
    // (the same schema/path — ConfigurationCommandBase.GetByParentJoin always joins within one path)
    // and reads its rows through the same record connector used for the primary container. Kept on the
    // connection (not the shared evaluator) because resolving a sibling container and reading it are
    // transport-specific — the evaluator only knows how to filter/join already-decoded rows. Returns
    // the resolved container alongside its rows (JoinedRowsResult) so RecordQueryEvaluator can validate
    // the join/filter columns and the loaded rows against the container's DECLARED field schema.
    private async Task<IGenericResult<JoinedRowsResult>> LoadJoinedRows(
        IDataContainer primaryContainer, string joinedContainerName, CancellationToken cancellationToken)
    {
        var joinedContainerResult = primaryContainer.Parent.Container(joinedContainerName);
        if (!joinedContainerResult.IsSuccess || joinedContainerResult.Value is null)
        {
            return GenericResult<JoinedRowsResult>.Failure(
                RecordQueryLog.JoinedContainerNotFound(Logger, joinedContainerName, primaryContainer.Parent.Name));
        }

        var joinedContainer = joinedContainerResult.Value;
        var readResult = await _recordConnector
            .Read(joinedContainer, joinedContainer.Path.PathValue, cancellationToken)
            .ConfigureAwait(false);
        if (!readResult.IsSuccess)
        {
            return readResult.ToNewResult<JoinedRowsResult>();
        }

        return GenericResult<JoinedRowsResult>.Success(new JoinedRowsResult(joinedContainer, readResult.Value!));
    }

    private async Task<IGenericResult<T>> ExecuteFind<T>(
        FileSystemFindCommand command,
        CancellationToken cancellationToken)
    {
        FileSystemConfigurationWriteLog.FindStarting(Logger, Name, command.Container.Name);

        var readResult = await _recordConnector
            .Read(command.Container, command.RelativePath, cancellationToken)
            .ConfigureAwait(false);
        if (!readResult.IsSuccess)
            return readResult.ToNewResult<T>();

        var found = _finder.Search(command, readResult.Value!);
        if (!found.IsSuccess)
            return found.ToNewResult<T>();

        var answer = _finder.Answer<T>(found.Value!, command.Container);
        if (answer.IsSuccess)
            FileSystemConfigurationWriteLog.FindCompleted(Logger, Name, command.Container.Name, found.Value!.Count);
        return answer;
    }

    private async Task<IGenericResult<T>> ExecuteInsert<T>(
        FileSystemWriteCommand command,
        CancellationToken cancellationToken)
    {
        var writeResult = await _configurationWriter.InsertRows(command, cancellationToken).ConfigureAwait(false);
        if (!writeResult.IsSuccess)
        {
            return writeResult.ToNewResult<T>();
        }

        // Why: the write returns the affected record count (int), the same shape an Insert command's
        // result carries; convert to T when T is int/object, otherwise fail loud.
        if (writeResult.Value is T typed)
        {
            return GenericResult<T>.Success(typed);
        }

        if (typeof(T) == typeof(object))
        {
            return GenericResult<T>.Success((T)(object)writeResult.Value);
        }

        return GenericResult<T>.Failure(
            FileSystemConnectionLog.FactoryValidationFailed(
                Logger,
                Name,
                $"write result of type 'int' cannot be materialized as '{typeof(T).Name}'"));
    }

    /// <inheritdoc />
    /// <inheritdoc />
    /// <remarks>
    /// A file store has no native transaction, so this is an undo log: writes are applied as they
    /// happen and every file they overwrote is put back if the unit rolls back. That is the guarantee
    /// the configuration write path actually needs — a record written by several statements must not
    /// survive half-written — and it is deliberately less than a database transaction offers, because
    /// it provides no isolation from another process writing the same store.
    /// <para>
    /// Why this exists at all: the domain configuration providers write every record inside a unit of
    /// work, so without one a file-backed configuration store cannot be written through its own
    /// provider at all.
    /// </para>
    /// </remarks>
    public Task<IGenericResult<IDataConnectionTransaction>> BeginTransaction(
        CancellationToken cancellationToken = default)
    {
        var unit = new FileSystemConnectionTransaction(this, _client, Root, Name, Logger);
        var attached = _client.AttachUndoLog(unit);

        return Task.FromResult(attached.IsSuccess
            ? GenericResult<IDataConnectionTransaction>.Success(unit)
            : attached.ToNewResult<IDataConnectionTransaction>());
    }

    /// <inheritdoc />
    public override Task<IGenericResult> TestConnection(CancellationToken cancellationToken = default)
    {
        return Directory.Exists(Root)
            ? Task.FromResult<IGenericResult>(GenericResult.Success())
            : Task.FromResult<IGenericResult>(
                GenericResult.Failure(
                    FileSystemConnectionLog.RootDoesNotExist(
                        Logger,
                        Name, Root)));
    }

    /// <summary>
    /// Performs a cheap liveness probe by verifying that the configured root path exists.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating whether the configured root exists.</returns>
    public Task<IGenericResult> Probe(CancellationToken cancellationToken = default)
    {
        FileSystemConnectionLog.ProbeStarting(Logger, Name, Root);

        if (!Directory.Exists(Root))
        {
            return Task.FromResult<IGenericResult>(
                GenericResult.Failure(
                    FileSystemConnectionLog.RootDoesNotExist(Logger, Name, Root)));
        }

        FileSystemConnectionLog.ProbeSucceeded(Logger, Name, Root);
        return Task.FromResult<IGenericResult>(GenericResult.Success());
    }
}
