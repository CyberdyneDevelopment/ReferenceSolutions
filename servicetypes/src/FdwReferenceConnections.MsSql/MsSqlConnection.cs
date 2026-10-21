using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Data.MsSql;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.RowSources.DataReader.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Connections.MsSql.ErrorHandlers;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.MsSql.Logging;
using FdwReferenceConnections.MsSql.Mapping;
using Fdw.Services.Connections.MsSql.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Fdw.Services.Connections.MsSql;


namespace FdwReferenceConnections.MsSql;

/// <summary>
/// SQL Server data connection.
/// Inherits from ConnectionBase which handles IDataCommand → SqlCommand translation.
/// Uses ADO.NET connection pooling: opens a new SqlConnection per operation from the
/// driver's internal pool, rather than holding a single connection open.
/// </summary>
[ExcludeFromCodeCoverage] // Excluded: requires SQL Server connection
public sealed partial class MsSqlConnection : ConnectionBase<SqlCommand, MsSqlConnectionConfiguration, MsSqlConnection>, ITransactionalDataConnection, IRecordSourceConnection, ISupportsHealthProbe, IPooledConnectionSource
{
    private readonly string _connectionString;
    private readonly ILogger<MsSqlConnection> _logger;
    // Why the ACCESSOR and not a captured IAuthenticationContext: the factory that builds this
    // connection is a DI Singleton, so a context captured at construction is captured once — at
    // composition time, when no request or execution flow exists — and stays null for the life of
    // the process. The accessor is AsyncLocal-backed, so reading .Current at PLAN TIME (inside
    // BuildSessionContextPlan, on the flow that is actually opening the connection) yields the
    // caller's real context. Nullable because a host whose connection kinds declare no session
    // contexts never registers one.
    private readonly IAuthenticationContextAccessor? _authenticationContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlConnection"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="configuration">The connection configuration.</param>
    /// <param name="connectionString">The connection string built by the factory.</param>
    /// <param name="authenticationContextAccessor">
    /// Optional ambient accessor the connection reads the calling principal from at plan time, for
    /// RLS SESSION_CONTEXT injection.
    /// </param>
    public MsSqlConnection(
        ILogger<MsSqlConnection> logger,
        MsSqlConnectionConfiguration configuration,
        string connectionString,
        IAuthenticationContextAccessor? authenticationContextAccessor = null)
        : base(logger, configuration)
    {
        _connectionString = connectionString;
        _logger = logger;
        _authenticationContextAccessor = authenticationContextAccessor;
    }

    /// <inheritdoc />
    /// <remarks>
    /// The SQL Server query translator builds the GROUP BY, the ORDER BY over a group or an aggregate, the page
    /// and the group count for a query that carries an aggregation (FDW-1023).
    /// </remarks>
    public override bool AggregatesInStore => true;

    /// <summary>
    /// Gets the translator for a command type using TypeCollection lookup.
    /// </summary>
    protected override IDataCommandTranslator<SqlCommand> GetTranslator(string commandType)
        => MsSqlDataCommandTranslators.ByName(commandType);

    /// <summary>
    /// Gets the connection type identifier.
    /// </summary>
    public static string ConnectionType => "MsSql";

    // Why: Configuration is non-public on services (FDW-454). Internal database accessor for
    // same-assembly schema-discovery callers (MsSqlSchemaCommands) that need the target db name.
    internal string Database => Configuration.Database;

    // Why: the script extension (FdwReferenceConnections.Script, FDW-1447) runs a script on this connection's own pooled
    // connections and logs where the connection logs.
    internal ILogger ScriptLogger => _logger;

    /// <summary>
    /// Gets the connection string for creating pooled connections.
    /// Internal to allow schema discovery and commands within this assembly.
    /// </summary>
    internal string ConnectionString => _connectionString;

    /// <summary>
    /// Executes a SqlCommand with a typed result using container schema for result materialization.
    /// ConnectionBase routes translated SqlCommand to this method.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="command">The SqlCommand to execute.</param>
    /// <param name="container">The container with schema metadata for converter-based materialization.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the typed execution outcome.</returns>
    protected override async Task<IGenericResult<T>> Execute<T>(
        SqlCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken)
    {
        // What was asked to run, before the session context can be put in front of it: every log and every
        // failure below names this text and this parameter count, never the batch that was sent.
        var commandText = command.CommandText;
        var parameterCount = command.Parameters.Count;
        var placement = SessionContextPlacement.Separate;

        MsSqlConnectionLogger.TraceExecuteEntry(Logger, commandText);

        try
        {
            // Open a pooled connection for this operation
            var sqlConnection = CreatePooledConnection();
            await using (sqlConnection.ConfigureAwait(false))
            {
                await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);

                // Assign the connection to the command
                command.Connection = sqlConnection;

                // A configuration save or retire answers with the key values of the rows it touched.
                var answersWithWriteResult = typeof(T) == typeof(ConfigurationWriteResult);

                // Determine if this is a query (SELECT) or command (INSERT/UPDATE/DELETE), on the text as it
                // was asked for: the batch below starts with the session context, not with SELECT.
                var isQuery = commandText.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase);

                // Every plain-text command carries its session context in its own batch, a read and a write
                // alike; a bulk copy and a stored procedure cannot, and are given the separate apply inside
                // PlaceSessionContext.
                var placed = await PlaceSessionContext(sqlConnection, command, cancellationToken).ConfigureAwait(false);
                if (!placed.IsSuccess || placed.Value is not { } placedContext)
                {
                    return placed.ToNewResult<T>();
                }

                placement = placedContext;

                MsSqlConnectionLogger.ExecutingSqlCommand(Logger, commandText, parameterCount);

                if (answersWithWriteResult)
                {
                    using var writeReader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                    placement.Applied(sqlConnection);
                    var writeResult = await ReadWriteResult(writeReader, Logger, cancellationToken).ConfigureAwait(false);
                    MsSqlConnectionLogger.SqlCommandExecuted(Logger, commandText, writeResult.Retired.Count + (writeResult.Written is null ? 0 : 1));
                    return GenericResult<T>.Success((T)(object)writeResult);
                }

                if (!isQuery)
                {
                    // For non-query commands, execute and return rows affected
                    var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    placement.Applied(sqlConnection);
                    MsSqlConnectionLogger.SqlCommandExecuted(Logger, commandText, rowsAffected);

                    // Return rowsAffected as T if possible, otherwise default
                    var result = ConvertScalarResult<T>(rowsAffected);
                    return GenericResult<T>.Success(result);
                }

                // For SELECT queries, use reader and map results
                using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                placement.Applied(sqlConnection);
                var mappedResult = await MapReaderToType<T>(reader, container, cancellationToken).ConfigureAwait(false);
                if (!mappedResult.IsSuccess)
                {
                    return mappedResult;
                }

                MsSqlConnectionLogger.SqlCommandExecuted(Logger, commandText, 0);
                return mappedResult;
            }
        }
        catch (SqlException ex)
        {
            MsSqlConnectionLogger.SqlExecutionError(Logger, commandText, ex.Message, ex.Number);
            return GenericResult<T>.Failure(placement.FailureMessages(_logger, ex, commandText));
        }
        catch (Exception ex)
        {
            MsSqlConnectionLogger.ExecutionException(Logger, commandText, ex.Message);
            return GenericResult<T>.Failure(MsSqlConnectionLogger.SqlExecutionFailedWithMessage(_logger, ex, commandText));
        }
    }

    /// <summary>
    /// Executes a data command, taking the bulk route when the command asks for it (FDW-1152): a bulk insert with more rows than
    /// the connection's <c>MaxSerialInsert</c>, or than one request can carry of the container's columns, is copied with
    /// SqlBulkCopy; every other command, and a bulk insert up to the limit, is translated and run as SQL.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="command">The data command.</param>
    /// <param name="container">The container the command runs against.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the typed execution outcome.</returns>
    public override async Task<IGenericResult<T>> Execute<T>(
        IDataCommand command, IDataContainer container, CancellationToken cancellationToken = default)
        => MsSqlBulkRoute.Takes(command, container, Configuration.MaxSerialInsert)
            ? await ExecuteBulk<T>(command, container, cancellationToken).ConfigureAwait(false)
            : await base.Execute<T>(command, container, cancellationToken).ConfigureAwait(false);

    // The bulk route: the rows are planned, a pooled connection is opened with the caller's session context set, and the rows are
    // copied. A database error is a coded failure like any other command's; nothing is thrown for one.
    private async Task<IGenericResult<T>> ExecuteBulk<T>(IDataCommand command, IDataContainer container, CancellationToken cancellationToken)
    {
        var plan = MsSqlBulkRoute.Plan(command, container, _logger, Configuration.Name, Configuration.MaxSerialInsert, inTransaction: false);
        if (!plan.IsSuccess || plan.Value is null)
            return plan.ToNewResult<T>();

        var copyText = $"BULK INSERT INTO {plan.Value.Destination}";
        try
        {
            var sqlConnection = CreatePooledConnection();
            await using (sqlConnection.ConfigureAwait(false))
            {
                await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
                var contextSet = await SetUserSessionContext(sqlConnection, cancellationToken).ConfigureAwait(false);
                if (contextSet.IsFailure)
                    return contextSet.ToNewResult<T>();

                MsSqlConnectionLogger.ExecutingSqlCommand(Logger, copyText, plan.Value.Rows.Rows.Count);
                var copied = await MsSqlBulkCopy.Write(plan.Value, sqlConnection, null, cancellationToken).ConfigureAwait(false);
                MsSqlConnectionLogger.SqlCommandExecuted(Logger, copyText, copied);
                return GenericResult<T>.Success(ConvertScalarResult<T>(copied));
            }
        }
        catch (SqlException ex)
        {
            MsSqlConnectionLogger.SqlExecutionError(Logger, copyText, ex.Message, ex.Number);
            return GenericResult<T>.Failure(SqlErrorHandlers.FailureMessages(_logger, ex, copyText));
        }
        catch (Exception ex)
        {
            MsSqlConnectionLogger.ExecutionException(Logger, copyText, ex.Message);
            return GenericResult<T>.Failure(MsSqlConnectionLogger.BulkCopyFailed(_logger, ex));
        }
    }

    /// <summary>Reads what a configuration save or retire returned.</summary>
    /// <remarks>
    /// The translator's OUTPUT tags every row with the kind of write that touched it
    /// (<see cref="ConfigurationWriteResult.KindColumn"/>): the rows a retire closed, and the row an
    /// insert wrote. A save runs both, so each has its own result set. A row with no kind is not one
    /// this contract describes, and refuses the read.
    /// </remarks>
    internal static async Task<ConfigurationWriteResult> ReadWriteResult(SqlDataReader reader, ILogger logger, CancellationToken cancellationToken)
    {
        var retired = new List<IReadOnlyDictionary<string, object?>>();
        Dictionary<string, object?>? written = null;
        do
        {
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var row = ReadCurrentRow(reader, logger);
                var kind = row.TryGetValue(ConfigurationWriteResult.KindColumn, out var value) ? value as string : null;
                row.Remove(ConfigurationWriteResult.KindColumn);
                if (string.Equals(kind, ConfigurationWriteResult.WrittenKind, StringComparison.Ordinal))
                    written = row;
                else if (string.Equals(kind, ConfigurationWriteResult.RetiredKind, StringComparison.Ordinal))
                    retired.Add(row);
                else
                    throw new InvalidOperationException(
                        $"A configuration write returned a row whose {ConfigurationWriteResult.KindColumn} is '{kind}'; expected '{ConfigurationWriteResult.RetiredKind}' or '{ConfigurationWriteResult.WrittenKind}'.");
            }
        }
        while (await reader.NextResultAsync(cancellationToken).ConfigureAwait(false));

        return new ConfigurationWriteResult(retired, written);
    }

    private static Dictionary<string, object?> ReadCurrentRow(SqlDataReader reader, ILogger logger)
    {
        var row = new Dictionary<string, object?>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < reader.FieldCount; i++)
            row[reader.GetName(i)] = SqlSpatialReader.ReadValue(reader, i, logger);
        return row;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IRecordSource<DataRecord>>> OpenRecordSource(
        IDataCommand command, IDataContainer container, CancellationToken cancellationToken = default)
    {
        // Translate IDataCommand -> SqlCommand exactly as the materializing Execute path does.
        var translator = GetTranslator(command.CommandType);
        var translationResult = await translator.Translate(command, container, cancellationToken).ConfigureAwait(false);
        if (!translationResult.IsSuccess || translationResult.Value is null)
        {
            return translationResult.ToNewResult<IRecordSource<DataRecord>>();
        }

        var sqlCommand = translationResult.Value;

        // What was asked to run, before the session context can be put in front of it (see Execute<T>).
        var commandText = sqlCommand.CommandText;
        var parameterCount = sqlCommand.Parameters.Count;
        var placement = SessionContextPlacement.Separate;
        SqlConnection? sqlConnection = null;
        SqlDataReader? reader = null;
        try
        {
            sqlConnection = CreatePooledConnection();
            await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
            sqlCommand.Connection = sqlConnection;

            MsSqlConnectionLogger.ExecutingSqlCommand(Logger, commandText, parameterCount);

            // A cursor is a read: its context rides in the batch that opens it.
            var placed = await PlaceSessionContext(sqlConnection, sqlCommand, cancellationToken).ConfigureAwait(false);
            if (!placed.IsSuccess || placed.Value is not { } placedContext)
            {
                await sqlConnection.DisposeAsync().ConfigureAwait(false);
                return placed.ToNewResult<IRecordSource<DataRecord>>();
            }

            placement = placedContext;

            reader = await sqlCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            placement.Applied(sqlConnection);

            // Why: the container's field children implement IDataField; CursorRecordSource builds ONE
            // RecordSchema flyweight from them, and every DataRecord exposes its values as a span over a
            // single object?[] — no per-row dictionary or repeated key strings. The returned source OWNS
            // the reader + connection (kept open while streaming) and releases them on Dispose.
            var fields = container.Schema.Fields.OfType<Fdw.Data.Abstractions.IDataField>().ToList();
            var cursor = new CursorRecordSource(new DataReaderRowSource(reader), fields, _logger);
            var scoped = new ConnectionScopedRecordSource(cursor, sqlCommand, reader, sqlConnection);
            reader = null;          // ownership transferred to 'scoped'; don't dispose in catch
            sqlConnection = null;
            return GenericResult<IRecordSource<DataRecord>>.Success(scoped);
        }
        catch (SqlException ex)
        {
            if (reader is not null) await reader.DisposeAsync().ConfigureAwait(false);
            if (sqlConnection is not null) await sqlConnection.DisposeAsync().ConfigureAwait(false);
            MsSqlConnectionLogger.SqlExecutionError(Logger, commandText, ex.Message, ex.Number);
            return GenericResult<IRecordSource<DataRecord>>.Failure(placement.FailureMessages(_logger, ex, commandText));
        }
        catch (Exception ex)
        {
            if (reader is not null) await reader.DisposeAsync().ConfigureAwait(false);
            if (sqlConnection is not null) await sqlConnection.DisposeAsync().ConfigureAwait(false);
            return GenericResult<IRecordSource<DataRecord>>.Failure(MsSqlConnectionLogger.SqlExecutionFailedWithMessage(_logger, ex, commandText));
        }
    }

    // Why: a streaming read keeps its SqlConnection + SqlDataReader open for the lifetime of the cursor
    // (the caller iterates lazily), so disposal can't happen at method exit like the materializing path.
    // This wrapper carries that ownership: disposing it (the caller MUST) releases the cursor, reader,
    // command, and pooled connection. Disposes are idempotent/null-safe.
    private sealed class ConnectionScopedRecordSource : IRecordSource<DataRecord>
    {
        private readonly IRecordSource<DataRecord> _inner;
        private readonly SqlCommand _command;
        private readonly SqlDataReader _reader;
        private readonly SqlConnection _connection;
        private bool _disposed;

        public ConnectionScopedRecordSource(
            IRecordSource<DataRecord> inner, SqlCommand command, SqlDataReader reader, SqlConnection connection)
        {
            _inner = inner;
            _command = command;
            _reader = reader;
            _connection = connection;
        }

        public RecordSchema Schema => _inner.Schema;

        public IEnumerable<IGenericResult<DataRecord>> Read() => _inner.Read();

        public IAsyncEnumerable<IGenericResult<DataRecord>> Read(CancellationToken cancellationToken = default)
            => _inner.Read(cancellationToken);

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _inner.Dispose();
            _reader.Dispose();
            _command.Dispose();
            _connection.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            await _inner.DisposeAsync().ConfigureAwait(false);
            await _reader.DisposeAsync().ConfigureAwait(false);
            await _command.DisposeAsync().ConfigureAwait(false);
            await _connection.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<global::System.Collections.Generic.IEnumerable<object>>> ExecuteRowsByType(
        SqlCommand command, IStorageContainer container, global::System.Type elementType, CancellationToken cancellationToken)
    {
        // What was asked to run, before the session context can be put in front of it (see Execute<T>).
        var commandText = command.CommandText;
        var parameterCount = command.Parameters.Count;
        var placement = SessionContextPlacement.Separate;

        MsSqlConnectionLogger.TraceExecuteEntry(Logger, commandText);
        try
        {
            // Open a pooled connection for this operation (sets RLS SESSION_CONTEXT, like Execute<T>).
            var sqlConnection = CreatePooledConnection();
            await using (sqlConnection.ConfigureAwait(false))
            {
                await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
                command.Connection = sqlConnection;

                MsSqlConnectionLogger.ExecutingSqlCommand(Logger, commandText, parameterCount);

                // Always a read, and the typed child-row read a configuration aggregate makes once per owner
                // row: the one path where the second round trip cost the most.
                var placed = await PlaceSessionContext(sqlConnection, command, cancellationToken).ConfigureAwait(false);
                if (!placed.IsSuccess || placed.Value is not { } placedContext)
                {
                    return placed.ToNewResult<global::System.Collections.Generic.IEnumerable<object>>();
                }

                placement = placedContext;

                using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                placement.Applied(sqlConnection);
                var rows = new List<object>();
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    // Why: per-row materialization is already Type-based (resolves the element's
                    // generated mapper by name) — no MakeGenericMethod / Activator.
                    var rowResult = MapReaderRowToObject(reader, elementType, container, Logger);
                    if (!rowResult.IsSuccess)
                    {
                        return rowResult.ToNewResult<global::System.Collections.Generic.IEnumerable<object>>();
                    }
                    rows.Add(rowResult.Value!);
                }
                return GenericResult<global::System.Collections.Generic.IEnumerable<object>>.Success(rows);
            }
        }
        catch (SqlException ex)
        {
            MsSqlConnectionLogger.SqlExecutionError(Logger, commandText, ex.Message, ex.Number);
            return GenericResult<global::System.Collections.Generic.IEnumerable<object>>.Failure(
                placement.FailureMessages(_logger, ex, commandText));
        }
        catch (Exception ex)
        {
            MsSqlConnectionLogger.ExecutionException(Logger, commandText, ex.Message);
            return GenericResult<global::System.Collections.Generic.IEnumerable<object>>.Failure(
                MsSqlConnectionLogger.SqlExecutionFailedWithMessage(_logger, ex, commandText));
        }
    }

    /// <summary>
    /// Executes a SqlCommand without a typed result.
    /// ConnectionBase routes translated SqlCommand to this method.
    /// </summary>
    /// <param name="command">The SqlCommand to execute.</param>
    /// <param name="container">The data container context for the operation.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the execution outcome.</returns>
    protected override async Task<IGenericResult> Execute(
        SqlCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken)
    {
        var result = await Execute<object>(command, container, cancellationToken).ConfigureAwait(false);
        if (result.IsSuccess)
        {
            return GenericResult.Success();
        }

        return result.ToNewResult<object>();
    }

    /// <summary>
    /// Internal static entry point used by <see cref="MsSqlDataConnectionTransaction"/> to reuse
    /// the same reader-to-type mapping logic without needing an <see cref="MsSqlConnection"/> instance.
    /// </summary>
    internal static Task<IGenericResult<T>> MapReaderToTypeInternal<T>(
        SqlDataReader reader, IStorageContainer container, ILogger logger, CancellationToken cancellationToken)
    {
        var targetType = typeof(T);
        if (IsCollectionType(targetType, out var itemType))
        {
            return MapReaderToTypeCollection<T>(reader, container, itemType!, logger, cancellationToken);
        }
        return MapReaderSingle<T>(reader, container, logger, cancellationToken);
    }

    private static async Task<IGenericResult<T>> MapReaderToTypeCollection<T>(
        SqlDataReader reader, IStorageContainer container, Type itemType, ILogger logger, CancellationToken cancellationToken)
    {
        if (IsDataRowType(itemType))
            return GenericResult<T>.Success((T)ConvertToCollectionType(
                await ReadDataRows(reader, logger, cancellationToken).ConfigureAwait(false), typeof(T), itemType));

        var listResult = CreateElementList(itemType, logger);
        if (!listResult.IsSuccess)
            return listResult.ToNewResult<T>();

        var list = listResult.Value!;
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var itemResult = MapReaderRowToObject(reader, itemType, container, logger);
            if (!itemResult.IsSuccess)
                return itemResult.ToNewResult<T>();
            list.Add(itemResult.Value);
        }

        return GenericResult<T>.Success((T)ConvertToCollectionType(list, typeof(T), itemType));
    }

    // Why: IDataRow is the framework's row — a query whose columns are known only at runtime (a DataSet
    // query, a preview) asks for it, and it needs no generated mapper because it carries its own schema.
    private static bool IsDataRowType(Type t) => t == typeof(global::Fdw.Data.DataContainers.Abstractions.IDataRow) || t == typeof(global::Fdw.Data.DataContainers.Abstractions.DataRow);

    // Why the schema is built once and shared: every row of a result set has the same columns, and
    // DataRow addresses its values positionally against that schema, so building one per row would be
    // both wasteful and a chance for the two to disagree. Column names and CLR types come from the
    // reader itself, which is the only thing that knows the shape of an ad-hoc result set.
    private static async Task<System.Collections.IList> ReadDataRows(
        SqlDataReader reader, ILogger logger, CancellationToken cancellationToken)
    {
        var fields = new ISchemaField[reader.FieldCount];
        for (var i = 0; i < reader.FieldCount; i++)
            fields[i] = new global::Fdw.Data.DataContainers.Abstractions.SchemaField(reader.GetName(i), SqlSpatialReader.GetFieldClrType(reader, i), i);

        var schema = global::Fdw.Data.DataContainers.Abstractions.DataSchema.FromFields(fields);
        var rows = new List<global::Fdw.Data.DataContainers.Abstractions.IDataRow>();

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var values = new object?[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; i++)
                values[i] = SqlSpatialReader.ReadValue(reader, i, logger);

            rows.Add(new global::Fdw.Data.DataContainers.Abstractions.DataRow(schema, values));
        }

        return rows;
    }

    // Why: reflection-free element-list construction (replaces Activator.CreateInstance(List<>.MakeGenericType)).
    // Mapped POCO element types use their generated mapper's CreateList(); the only other element type that
    // reaches here is Dictionary<string, object?> (data-preview rows), which has no [GenerateMapper] and is
    // constructed directly. Any other unmapped element type fails loud — there is no reflection fallback.
    private static IGenericResult<System.Collections.IList> CreateElementList(Type itemType, ILogger logger)
    {
        var mapper = PocoMapperCollection.ByName(itemType.Name);
        if (mapper != PocoMapperCollection.NotFound)
            return GenericResult<System.Collections.IList>.Success(mapper.CreateList());

        if (itemType.IsGenericType && itemType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            return GenericResult<System.Collections.IList>.Success(new List<Dictionary<string, object?>>());

        var message = MsSqlConnectionLogger.NoMapperFound(logger, itemType.Name, itemType.FullName ?? itemType.Name);
        return GenericResult<System.Collections.IList>.Failure(message);
    }

    private static async Task<IGenericResult<T>> MapReaderSingle<T>(
        SqlDataReader reader, IStorageContainer container, ILogger logger, CancellationToken cancellationToken)
    {
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var itemResult = MapReaderRowToObject(reader, typeof(T), container, logger);
            return itemResult.IsSuccess ? GenericResult<T>.Success((T)itemResult.Value!) : itemResult.ToNewResult<T>();
        }

        return GenericResult<T>.Success(default!);
    }

    /// <summary>
    /// Maps SqlDataReader results to type T using container schema and converters.
    /// </summary>
    private async Task<IGenericResult<T>> MapReaderToType<T>(SqlDataReader reader, IStorageContainer container, CancellationToken cancellationToken)
    {
        var targetType = typeof(T);

        // Handle collection types (IEnumerable<TItem>, List<TItem>, TItem[], etc.)
        if (IsCollectionType(targetType, out var itemType))
        {
            if (IsDataRowType(itemType!))
                return GenericResult<T>.Success((T)ConvertToCollectionType(
                    await ReadDataRows(reader, _logger, cancellationToken).ConfigureAwait(false), targetType, itemType!));

            var listResult = CreateElementList(itemType!, _logger);
            if (!listResult.IsSuccess)
                return listResult.ToNewResult<T>();

            var list = listResult.Value!;
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var itemResult = MapReaderRowToObject(reader, itemType!, container, _logger);
                if (!itemResult.IsSuccess)
                    return itemResult.ToNewResult<T>();
                list.Add(itemResult.Value);
            }

            // Convert to requested collection type
            return GenericResult<T>.Success((T)ConvertToCollectionType(list, targetType, itemType!));
        }

        // Handle single object
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var itemResult = MapReaderRowToObject(reader, targetType, container, _logger);
            return itemResult.IsSuccess ? GenericResult<T>.Success((T)itemResult.Value!) : itemResult.ToNewResult<T>();
        }

        // No rows, return default
        return GenericResult<T>.Success(default!);
    }

    /// <summary>
    /// Maps a single SqlDataReader row to an object.
    /// Uses generated POCO mappers - NO REFLECTION FALLBACK.
    /// </summary>
    private static IGenericResult<object> MapReaderRowToObject(
        SqlDataReader reader,
        Type targetType,
        IStorageContainer container,
        ILogger logger)
    {
        // Handle primitive types (single column, direct mapping)
        if (targetType.IsPrimitive || targetType == typeof(string) || targetType == typeof(DateTime) ||
            targetType == typeof(decimal) || targetType == typeof(Guid))
        {
            return GenericResult<object>.Success(ConvertValue(reader.GetValue(0), targetType));
        }

        // Handle Dictionary<string, object?> (used by data preview endpoints).
        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            var dict = new Dictionary<string, object?>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < reader.FieldCount; i++)
            {
                dict[reader.GetName(i)] = SqlSpatialReader.ReadValue(reader, i, logger);
            }

            return GenericResult<object>.Success(dict);
        }

        // Try generated POCO mapper (required for complex types)
        // ByName looks up by the short type name (e.g., "DataSetEntity")
        // Note: ByName returns NotFound sentinel if not found, NOT null
        var mapper = PocoMapperCollection.ByName(targetType.Name);
        if (mapper == PocoMapperCollection.NotFound)
        {
            var message = MsSqlConnectionLogger.NoMapperFound(logger, targetType.Name, targetType.FullName ?? targetType.Name);
            return GenericResult<object>.Failure(message);
        }

        var mappingResult = mapper.MapFromReader(reader, container);
        if (!mappingResult.IsSuccess)
        {
            var message = MsSqlConnectionLogger.MappingFailed(logger, targetType.Name, mappingResult.CurrentMessage ?? "Unknown error");
            return GenericResult<object>.Failure(message);
        }

        return GenericResult<object>.Success(mappingResult.Value!);
    }

    /// <summary>
    /// Creates a mapping context for efficient row-by-row processing.
    /// Call this ONCE before reading any rows to pre-compute ordinals and converters.
    /// </summary>
    /// <param name="reader">The SqlDataReader to read from.</param>
    /// <param name="container">The container with schema metadata.</param>
    /// <returns>A pre-computed mapping context for use with MapReaderRowToDictionary.</returns>
    internal static RowMappingContext CreateMappingContext(SqlDataReader reader, IStorageContainer container)
        => RowMappingContext.Create(reader, container);

    /// <summary>
    /// Maps SqlDataReader row to dictionary using pre-computed context.
    /// Uses pooled dictionaries and cached lookups for high performance.
    /// </summary>
    /// <param name="reader">The SqlDataReader positioned on a row.</param>
    /// <param name="ctx">The pre-computed mapping context.</param>
    /// <param name="logger">Logger for a geography/geometry column's parse trace/failures.</param>
    /// <returns>A dictionary containing the row values.</returns>
    internal static Dictionary<string, object?> MapReaderRowToDictionary(SqlDataReader reader, RowMappingContext ctx, ILogger<MsSqlConnection>? logger = null)
    {
        logger ??= NullLogger<MsSqlConnection>.Instance;
        var dict = DictionaryPool.Rent(ctx.FieldCount);

        for (int i = 0; i < ctx.FieldCount; i++)
        {
            var ordinal = ctx.FieldOrdinals[i];
            var name = ctx.FieldNames[i];

            if (ordinal < 0 || reader.IsDBNull(ordinal))
            {
                dict[name] = null;
                continue;
            }

            var dbValue = SqlSpatialReader.ReadValue(reader, ordinal, logger);
            dict[name] = ctx.Converters[i]?.ToClr(dbValue) ?? dbValue;
        }

        return dict;
    }

    /// <summary>
    /// Returns a dictionary to the pool for reuse.
    /// Call this after you're done with a dictionary from MapReaderRowToDictionary.
    /// </summary>
    /// <param name="dict">The dictionary to return.</param>
    internal static void ReturnDictionary(Dictionary<string, object?> dict)
        => DictionaryPool.Return(dict);

    /// <summary>
    /// Converts a value to the target type.
    /// </summary>
    private static object ConvertValue(object value, Type targetType)
    {
        var underlyingType = Nullable.GetUnderlyingType(targetType);

        if (value == null || value == DBNull.Value)
        {
            // Why: NULL is valid only for a reference type or Nullable<T>. Mapping NULL into a
            // non-nullable value type would silently fabricate default(T) (0/false/Guid.Empty/...) —
            // a fallback that hides the NULL. Fail loud instead (NO-FALLBACKS rule); the read path's
            // try/catch turns this into a structured Failure.
            if (!targetType.IsValueType || underlyingType is not null)
                return null!;

            throw new InvalidOperationException(
                $"Cannot map a NULL database value to non-nullable type '{targetType.Name}'.");
        }

        var effectiveType = underlyingType ?? targetType;

        // Direct assignment if types match
        if (effectiveType.IsInstanceOfType(value))
        {
            return value;
        }

        // Why: a failed conversion is a real type mismatch — surface it (Convert.ChangeType throws)
        // rather than swallowing it into default(T), which would silently corrupt the row.
        return Convert.ChangeType(value, effectiveType, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Converts scalar result (like rowsAffected) to type T.
    /// </summary>
    internal static T ConvertScalarResult<T>(int value)
    {
        var targetType = typeof(T);

        if (targetType == typeof(int))
            return (T)(object)value;

        if (targetType == typeof(long))
            return (T)(object)(long)value;

        if (targetType == typeof(bool))
            return (T)(object)(value > 0);

        return default!;
    }

    /// <summary>
    /// Determines if a type is a collection type and extracts the item type.
    /// </summary>
    private static bool IsCollectionType(Type type, out Type? itemType)
    {
        // Array
        if (type.IsArray)
        {
            itemType = type.GetElementType();
            return true;
        }

        // Generic IEnumerable<T>, List<T>, etc.
        if (type.IsGenericType)
        {
            var genericTypeDef = type.GetGenericTypeDefinition();
            if (genericTypeDef == typeof(IEnumerable<>) ||
                genericTypeDef == typeof(List<>) ||
                genericTypeDef == typeof(IList<>) ||
                genericTypeDef == typeof(ICollection<>))
            {
                itemType = type.GetGenericArguments()[0];
                return true;
            }
        }

        // Implements IEnumerable<T>
        var enumerableInterface = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumerableInterface != null)
        {
            itemType = enumerableInterface.GetGenericArguments()[0];
            return true;
        }

        itemType = null;
        return false;
    }

    /// <summary>
    /// Converts a list to the requested collection type.
    /// </summary>
    private static object ConvertToCollectionType(System.Collections.IList list, Type targetType, Type itemType)
    {
        // Array
        if (targetType.IsArray)
        {
            var array = Array.CreateInstance(itemType, list.Count);
            list.CopyTo(array, 0);
            return array;
        }

        // Already a compatible list
        if (targetType.IsAssignableFrom(list.GetType()))
        {
            return list;
        }

        // IEnumerable<T>, IList<T>, etc. - return the list
        return list;
    }

    /// <summary>
    /// Tests connectivity by opening and closing a pooled connection.
    /// With ADO.NET pooling, this verifies the server is reachable without
    /// holding a connection open.
    /// </summary>
    public async Task<IGenericResult> Connect(CancellationToken cancellationToken = default)
    {
        MsSqlConnectionLogger.TraceConnectEntry(Logger, Configuration.Server, Configuration.Database);

        try
        {
            var sqlConnection = CreatePooledConnection();
            await using (sqlConnection.ConfigureAwait(false))
            {
                MsSqlConnectionLogger.OpeningConnection(Logger, ConnectionType);
                await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);

                // The context is the connectivity test here: one that did not land is a failed connect.
                var contextSet = await SetUserSessionContext(sqlConnection, cancellationToken).ConfigureAwait(false);
                if (contextSet.IsFailure)
                    return contextSet;

                MsSqlConnectionLogger.TraceConnectionOpened(Logger, sqlConnection.DataSource, sqlConnection.Database);
                MsSqlConnectionLogger.ConnectionOpened(Logger, ConnectionType);
                return GenericResult.Success();
            }
        }
        catch (SqlException ex)
        {
            MsSqlConnectionLogger.ConnectionFailed(Logger, ConnectionType, ex.Message);
            return GenericResult.Failure(
                SqlErrorHandlers.FailureMessages(_logger, ex, $"CONNECT {Configuration.Server}/{Configuration.Database}"));
        }
        catch (Exception ex)
        {
            MsSqlConnectionLogger.ConnectionFailed(Logger, ConnectionType, ex.Message);
            return GenericResult.Failure(MsSqlConnectionLogger.ConnectFailed(_logger, ex));
        }
    }

    /// <summary>
    /// No-op with pooled connections. Individual connections are returned to the pool
    /// automatically when disposed after each operation.
    /// </summary>
    public static Task<IGenericResult> Disconnect(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IGenericResult>(GenericResult.Success());
    }

    /// <summary>
    /// Tests the SQL Server connection by opening and closing a pooled connection.
    /// </summary>
    public override Task<IGenericResult> TestConnection(CancellationToken cancellationToken = default)
    {
        MsSqlConnectionLogger.TraceTestConnectionEntry(Logger);
        return Connect(cancellationToken);
    }

    /// <summary>
    /// Performs a cheap liveness probe against SQL Server by executing <c>SELECT 1</c>
    /// on a pooled connection.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating whether the probe succeeded.</returns>
    public async Task<IGenericResult> Probe(CancellationToken cancellationToken = default)
    {
        var connectionName = Configuration.Name;
        MsSqlConnectionLogger.TraceProbeEntry(Logger, connectionName);

        try
        {
            var sqlConnection = CreatePooledConnection();
            await using (sqlConnection.ConfigureAwait(false))
            {
                await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);

                var contextSet = await SetUserSessionContext(sqlConnection, cancellationToken).ConfigureAwait(false);
                if (contextSet.IsFailure)
                    return contextSet;

                var command = new SqlCommand("SELECT 1", sqlConnection) { CommandTimeout = 5 };
                await using (command.ConfigureAwait(false))
                {
                    await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                }
            }

            MsSqlConnectionLogger.ProbeSucceeded(Logger, connectionName);
            return GenericResult.Success();
        }
        catch (SqlException ex)
        {
            MsSqlConnectionLogger.SqlExecutionError(Logger, "SELECT 1", ex.Message, ex.Number);
            return GenericResult.Failure(SqlErrorHandlers.FailureMessages(_logger, ex, "SELECT 1"));
        }
        catch (Exception ex)
        {
            MsSqlConnectionLogger.ExecutionException(Logger, "SELECT 1", ex.Message);
            return GenericResult.Failure(MsSqlConnectionLogger.SqlExecutionFailedWithMessage(_logger, ex, "SELECT 1"));
        }
    }

    /// <summary>
    /// No managed resources to dispose — pooled connections are returned to the pool
    /// individually after each operation.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    public override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }

    /// <summary>
    /// Creates a new SqlConnection from the stored connection string.
    /// ADO.NET pools connections by connection string. Token-based auth
    /// (managed identity, AzureCli) is handled by SqlClient via
    /// Authentication=ActiveDirectoryDefault in the connection string —
    /// no manual AccessToken assignment needed.
    /// </summary>
    /// <inheritdoc />
    // Why: explicit implementation so the raw-ADO seam is reachable through IPooledConnectionSource
    // by callers in other assemblies (the data vault) without widening this class's own surface.
    System.Data.Common.DbConnection IPooledConnectionSource.CreatePooledConnection()
        => CreatePooledConnection();

    internal SqlConnection CreatePooledConnection()
    {
        return new SqlConnection(_connectionString);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IDataConnectionTransaction>> BeginTransaction(
        CancellationToken cancellationToken = default)
    {
        // Why: MsSqlConnectionConfiguration.Name is explicit-interface on IGenericConfiguration.
        // Cast once and cache to avoid repeated boxing.
        var connectionName = Configuration.Name;
        MsSqlConnectionLogger.TransactionStarted(_logger, connectionName);
        try
        {
            var sqlConnection = CreatePooledConnection();
            await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);

            // Set once for the whole unit. One that did not land is a connection nothing may run on, and it
            // is not handed to a transaction that would hold it open.
            var contextSet = await SetUserSessionContext(sqlConnection, cancellationToken).ConfigureAwait(false);
            if (contextSet.IsFailure)
            {
                await sqlConnection.DisposeAsync().ConfigureAwait(false);
                return contextSet.ToNewResult<IDataConnectionTransaction>();
            }

            var transaction = await sqlConnection.BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false) as SqlTransaction;
            // Why: BeginTransactionAsync on SqlConnection always returns SqlTransaction;
            // the cast is safe and avoids an interface indirection in the hot path.
            return GenericResult<IDataConnectionTransaction>.Success(
                new MsSqlDataConnectionTransaction(sqlConnection, transaction!, _logger, connectionName, Configuration.MaxSerialInsert));
        }
        catch (Exception ex)
        {
            return GenericResult<IDataConnectionTransaction>.Failure(
                MsSqlConnectionLogger.TransactionStartFailed(_logger, connectionName, ex.Message));
        }
    }

    /// <summary>
    /// Computes WHICH SESSION_CONTEXT keys the governing session context will set, from the calling
    /// principal read off the ambient accessor at plan time. Pure decision logic — no SQL, no I/O —
    /// so the gate is unit-testable without a live SQL Server connection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The decision itself belongs to the reference scheme's options
    /// (<see cref="MsSqlSessionContextTypes"/>), which document their contracts with
    /// <c>security.fn_TenantFilter</c> as deployed. In summary: an explicit system elevation sets
    /// nothing at all (Mode 1 keys off <c>UserId</c> being NULL); a real, authenticated,
    /// <see cref="Guid"/>-identified user gets their own key set; anything else — no context
    /// established, or one whose <c>UserId</c> is not <see cref="Guid"/>-parseable — gets the
    /// reserved <see cref="AuthConstants.NoAccessPrincipalId"/>.
    /// </para>
    /// <para>
    /// The app NEVER sends a null <c>SESSION_CONTEXT('UserId')</c> except via the explicit system
    /// option. That closes the prior fail-OPEN gap where an unestablished or non-Guid context fell
    /// through to the SAME null-UserId bypass reserved for system connections.
    /// </para>
    /// <para>
    /// The deny principal is <b>not</b> denied everywhere: it is denied every tenant-scoped branch,
    /// while <c>fn_TenantFilter.sql:48-51</c> still admits shared/system rows
    /// (<c>TenantId IS NULL AND VisibilityGroupId IS NULL</c>) with no session-context test — the
    /// same rows any other tenant-less caller sees.
    /// </para>
    /// </remarks>
    internal SessionContextPlan BuildSessionContextPlan()
        => SelectSessionContext().Plan(_authenticationContextAccessor?.Current);

    /// <summary>
    /// Resolves WHICH session context governs this connection's next open, reading the calling
    /// principal from the ambient accessor at plan time.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the participation seam.</b> Today every MsSql connection uses the reference
    /// scheme, selected from the calling principal. When a per-connection <c>SessionContextType</c>
    /// discriminator lands, the selection becomes a lookup of the connection's own declared context
    /// instead — one call site changes, and nothing above it assumes a session context always
    /// applies.
    /// </para>
    /// <para>
    /// Why the accessor is read HERE and not captured in the constructor: the factory that built
    /// this connection is a Singleton. A context captured at construction is captured before any
    /// request or execution flow exists, so it is permanently null and every connection computes the
    /// deny-everywhere plan. Reading <c>.Current</c> on the flow that is actually opening the
    /// connection is what makes the plan reflect the real caller.
    /// </para>
    /// </remarks>
    internal MsSqlSessionContextBase SelectSessionContext()
        => MsSqlSessionContextTypes.For(_authenticationContextAccessor?.Current);

    /// <summary>
    /// Applies the governing session context's SESSION_CONTEXT keys to an open connection.
    /// Must be called after OpenAsync on every pooled connection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method owns only the <i>call</i>: which context governs is
    /// <see cref="SelectSessionContext"/>'s decision, what it writes is that option's contract. The
    /// reference scheme's three options are documented on <see cref="MsSqlSessionContextTypes"/>,
    /// each as a clause-level contract with <c>security.fn_TenantFilter</c> as deployed. A consumer
    /// running a different row-level-security design replaces that whole collection, so nothing
    /// here may assume a particular option's semantics.
    /// </para>
    /// <para>
    /// Note in particular that the deny option is <b>not</b> deny-everywhere: it is denied every
    /// tenant-scoped branch, but <c>fn_TenantFilter.sql:48-51</c> admits shared/system rows
    /// (<c>TenantId IS NULL AND VisibilityGroupId IS NULL</c>) with no session-context test at all.
    /// </para>
    /// </remarks>
    // Why: internal (not private) so the RLS acceptance test can compose CreatePooledConnection +
    // SetUserSessionContext — the exact production session-context path — now that the shared public
    // GetOpenSqlConnection seam is removed. Not part of the public surface.
    // A failure is returned, never thrown, and the connection must not be used after one: each caller returns
    // it from inside the scope that disposes the connection (FDW-980).
    internal Task<IGenericResult> SetUserSessionContext(SqlConnection connection, CancellationToken cancellationToken)
    {
        // Why resolved once and reused: SelectSessionContext reads the ambient accessor, and the
        // option must build its plan from the SAME context it was selected for.
        var authenticationContext = _authenticationContextAccessor?.Current;
        var sessionContext = MsSqlSessionContextTypes.For(authenticationContext);

        return sessionContext.Apply(
            connection,
            sessionContext.Plan(authenticationContext),
            Logger,
            cancellationToken);
    }

    // Why one method: every path that runs a translated command on a pooled connection meets the session
    // context HERE, so a DataCommand is given the same context however it is executed (FDW-977). A command
    // that can carry it (plain text, which leaves out a bulk copy and a stored procedure) sends the context
    // in its own batch, so a command is one round trip instead of two. One that cannot gets the separate
    // apply it always had.
    // The command's text and parameters are changed when it carries the context: the caller keeps the
    // text it asked to run, and logs that.
    // internal only so the offline tests can reach it; not part of the connection's surface.
    internal async Task<IGenericResult<SessionContextPlacement>> PlaceSessionContext(
        SqlConnection sqlConnection, SqlCommand command, CancellationToken cancellationToken)
    {
        if (BatchedSessionContext.CanCarry(command))
        {
            var authenticationContext = _authenticationContextAccessor?.Current;
            var sessionContext = MsSqlSessionContextTypes.For(authenticationContext);
            var attached = BatchedSessionContext.Attach(
                sessionContext, sessionContext.Plan(authenticationContext), command, Logger);

            return attached.IsSuccess && attached.Value is { } batch
                ? GenericResult<SessionContextPlacement>.Success(SessionContextPlacement.Batched(batch))
                : attached.ToNewResult<SessionContextPlacement>();
        }

        var contextSet = await SetUserSessionContext(sqlConnection, cancellationToken).ConfigureAwait(false);
        return contextSet.IsSuccess
            ? GenericResult<SessionContextPlacement>.Success(SessionContextPlacement.Separate)
            : contextSet.ToNewResult<SessionContextPlacement>();
    }

    // Where a command's session context went: in its own batch (a BatchedSessionContext), or already set
    // ahead of it on the connection. It only lets the execution paths ask two questions of the outcome
    // without knowing which it was. internal only so the offline tests can reach it; not part of the
    // connection's surface.
    internal sealed class SessionContextPlacement
    {
        public static readonly SessionContextPlacement Separate = new(null);

        private readonly BatchedSessionContext? _batch;

        private SessionContextPlacement(BatchedSessionContext? batch) => _batch = batch;

        public static SessionContextPlacement Batched(BatchedSessionContext batch) => new(batch);

        public bool IsBatched => _batch is not null;

        // Said once the batch has started without a context failure, as Apply says it once the keys are set.
        public void Applied(SqlConnection connection) => _batch?.Applied(connection);

        // What a failed command says. A context that could not be set, in its command's batch, says what Apply
        // says when it fails on its own, so a caller is given the same failure whichever way the context ran;
        // any other SQL error says what its handler says, and an error with no handler carries the flattened
        // exception. Nothing is thrown for a database error.
        public IReadOnlyList<IGenericMessage> FailureMessages(ILogger logger, SqlException exception, string commandText)
            => _batch is not null && BatchedSessionContext.IsContextFailure(exception)
                ? MsSqlSessionContextBase.ContextFailureMessages(logger, exception, commandText)
                : SqlErrorHandlers.FailureMessages(logger, exception, commandText);
    }

    /// <summary>
    /// Queries all active (IsCurrent=1, IsDeleted=0) rows from a configuration table.
    /// Used by the configuration provider during startup before the full DataCommand
    /// infrastructure is available. SQL construction is encapsulated here so the provider
    /// never builds raw SQL strings.
    /// </summary>
    /// <param name="schema">The database schema (e.g., "conn", "sec", "sched").</param>
    /// <param name="tableName">The table name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing rows as column name → value dictionaries.
    /// Returns an empty list (success) if the table does not exist (SqlException 208).</returns>
    // Why: Startup query methods encapsulate SQL construction AND SqlException handling
    // so the configuration provider is free of Microsoft.Data.SqlClient references.
    internal async Task<IGenericResult<List<Dictionary<string, object?>>>> QueryConfigurationTable(
        string schema,
        string tableName,
        CancellationToken cancellationToken = default)
    {
        var sql = string.Format(
            CultureInfo.InvariantCulture,
            "SELECT * FROM [{0}].[{1}] WHERE [IsCurrent] = 1 AND [IsDeleted] = 0",
            schema,
            tableName);

        try
        {
            return await QueryRowsInternal(sql, cancellationToken).ConfigureAwait(false);
        }
        // Why: Error 208 = "Invalid object name" — table doesn't exist in this schema.
        // Expected when a configuration type's table lives in a different per-domain schema
        // (e.g. "conn" vs "sec") than the one queried. Return empty success, not failure.
        // FDW014 suppressed: intentional — 208 is an expected condition, not an error to propagate.
#pragma warning disable FDW014
        catch (SqlException ex) when (ex.Number == 208)
        {
            return GenericResult<List<Dictionary<string, object?>>>.Success([]);
        }
#pragma warning restore FDW014
        catch (SqlException ex)
        {
            MsSqlConnectionLogger.SqlExecutionError(Logger, sql, ex.Message, ex.Number);
            return GenericResult<List<Dictionary<string, object?>>>.Failure(SqlErrorHandlers.FailureMessages(_logger, ex, sql));
        }
    }

    /// <summary>
    /// Internal: executes a raw SQL query and returns rows as dictionaries.
    /// </summary>
    private async Task<IGenericResult<List<Dictionary<string, object?>>>> QueryRowsInternal(
        string sql,
        CancellationToken cancellationToken)
    {
        var results = new List<Dictionary<string, object?>>();

        var connection = CreatePooledConnection();
        await using (connection.ConfigureAwait(false))
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var contextSet = await SetUserSessionContext(connection, cancellationToken).ConfigureAwait(false);
            if (contextSet.IsFailure)
                return contextSet.ToNewResult<List<Dictionary<string, object?>>>();

            var command = new SqlCommand(sql, connection) { CommandTimeout = 30 };
            await using (command.ConfigureAwait(false))
            {
                var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                await using (reader.ConfigureAwait(false))
                {
                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            row[reader.GetName(i)] = await reader.IsDBNullAsync(i, cancellationToken).ConfigureAwait(false) ? null : SqlSpatialReader.ReadValue(reader, i, Logger);
                        }
                        results.Add(row);
                    }
                }
            }
        }

        return GenericResult<List<Dictionary<string, object?>>>.Success(results);
    }

}
