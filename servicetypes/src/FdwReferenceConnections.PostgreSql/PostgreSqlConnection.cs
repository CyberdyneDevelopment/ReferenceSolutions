using System;
using Fdw.Data.RowSources.DataReader.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Abstractions.Logging;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Data.PostgreSql;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.PostgreSql.Logging;
using Microsoft.Extensions.Logging;
using Npgsql;

using Fdw.Services.Connections.PostgreSql;


namespace FdwReferenceConnections.PostgreSql;

/// <summary>
/// PostgreSQL data connection.
/// Holds a single long-lived <see cref="NpgsqlConnection"/> that is opened on first use.
/// </summary>
[ExcludeFromCodeCoverage] // Excluded: requires PostgreSQL connection
public sealed class PostgreSqlConnection : ConnectionBase<NpgsqlCommand, PostgreSqlConnectionConfiguration, PostgreSqlConnection>, ISupportsHealthProbe, IRecordSourceConnection
{
    private readonly NpgsqlConnection _npgsqlConnection;
    private readonly ILogger<PostgreSqlConnection> _logger;
    // Why: After config-split, Name lives on the parent IConnectionImplementationConfiguration header, not on the typed body.
    // The factory extracts it and passes it explicitly so logging uses the correct connection name.
    private readonly string _connectionName;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlConnection"/> class.
    /// </summary>
    /// <param name="logger">Logger for connection operations.</param>
    /// <param name="configuration">The typed PostgreSQL connection configuration.</param>
    /// <param name="npgsqlConnection">The underlying Npgsql connection.</param>
    /// <param name="connectionName">The connection name from the parent IConnectionImplementationConfiguration header.</param>
    public PostgreSqlConnection(
        ILogger<PostgreSqlConnection> logger,
        PostgreSqlConnectionConfiguration configuration,
        NpgsqlConnection npgsqlConnection,
        string connectionName)
        : base(logger, configuration)
    {
        _npgsqlConnection = npgsqlConnection;
        _logger = logger;
        _connectionName = connectionName;
    }

    /// <summary>
    /// Gets the translator for a command type using TypeCollection lookup.
    /// </summary>
    protected override IDataCommandTranslator<NpgsqlCommand> GetTranslator(string commandType)
        => PostgreSqlDataCommandTranslators.ByName(commandType);

    /// <summary>
    /// Gets the connection type identifier.
    /// </summary>
    public static string ConnectionType => "PostgreSql";

    /// <summary>
    /// Gets a value indicating whether the connection is currently open.
    /// </summary>
    public bool IsConnected => _npgsqlConnection.State == ConnectionState.Open;

    /// <summary>
    /// Gets the connection string from the underlying NpgsqlConnection.
    /// </summary>
    public string ConnectionString => _npgsqlConnection.ConnectionString;

    /// <summary>
    /// Gets the underlying NpgsqlConnection for direct use by schema discoverers.
    /// </summary>
    public NpgsqlConnection InnerConnection => _npgsqlConnection;

    /// <summary>
    /// Executes a NpgsqlCommand with a typed result using container schema for result materialization.
    /// ConnectionBase routes translated NpgsqlCommand to this method.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="command">The NpgsqlCommand to execute.</param>
    /// <param name="container">The container with schema metadata for converter-based materialization.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the typed execution outcome.</returns>
    protected override async Task<IGenericResult<T>> Execute<T>(
        NpgsqlCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken)
    {
        PostgreSqlConnectionLog.TraceExecuteEntry(_logger, _connectionName);

        try
        {
            // Open the connection if not already open
            if (_npgsqlConnection.State != ConnectionState.Open)
            {
                await _npgsqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
                PostgreSqlConnectionLog.ConnectionOpened(_logger, _connectionName);
            }

            command.Connection = _npgsqlConnection;

            PostgreSqlConnectionLog.ExecutingSqlCommand(_logger, command.CommandText, command.Parameters.Count);

            // Determine if this is a query (SELECT) or command (INSERT/UPDATE/DELETE)
            var isQuery = command.CommandText.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase);

            if (!isQuery)
            {
                // For non-query commands, execute and return rows affected
                var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                PostgreSqlConnectionLog.SqlCommandExecuted(_logger, command.CommandText, rowsAffected);
                return GenericResult<T>.Success(ConvertScalarResult<T>(rowsAffected));
            }

            // For SELECT queries, use reader and map results
            using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            var mappedResult = await MapReaderToType<T>(reader, container, cancellationToken).ConfigureAwait(false);
            if (!mappedResult.IsSuccess)
            {
                return mappedResult;
            }

            PostgreSqlConnectionLog.SqlCommandExecuted(_logger, command.CommandText, 0);

            return mappedResult;
        }
        catch (NpgsqlException ex)
        {
            return GenericResult<T>.Failure(
                ExceptionResultExtensions.FlattenException(ex).Prepend(PostgreSqlConnectionLog.QueryFailed(_logger, ex, _connectionName)));
        }
        catch (Exception ex)
        {
            return GenericResult<T>.Failure(
                PostgreSqlConnectionLog.SqlExecutionFailed(_logger, ex, command.CommandText));
        }
    }

    /// <summary>
    /// Executes a NpgsqlCommand without a typed result.
    /// </summary>
    protected override async Task<IGenericResult> Execute(
        NpgsqlCommand command,
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
    /// Executes a data command, taking the bulk route when the command asks for it (FDW-1152): a bulk insert with more rows than
    /// the connection's <c>MaxSerialInsert</c>, or than one command can carry of the container's columns, is streamed through a binary COPY; every other command, and a bulk insert up to the
    /// limit, is translated and run as SQL. This connection has no unit of work, so the COPY is never inside one.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="command">The data command.</param>
    /// <param name="container">The container the command runs against.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the typed execution outcome.</returns>
    public override async Task<IGenericResult<T>> Execute<T>(
        IDataCommand command, IDataContainer container, CancellationToken cancellationToken = default)
        => TakesBulkRoute(command, container)
            ? await ExecuteBulk<T>(command, container, cancellationToken).ConfigureAwait(false)
            : await base.Execute<T>(command, container, cancellationToken).ConfigureAwait(false);

    // Asked first: a command that would not take the bulk route even if no row could run serially has no use for it, and its
    // container is not sized. Every data command answers; only a bulk insert with rows answers yes.
    private const int NoSerialRows = 0;

    // The route is chosen by the command's rows against the most a serial insert takes (FDW-1173).
    private bool TakesBulkRoute(IDataCommand command, IStorageContainer container)
        => command is IDatabaseConnectionCommand routed
           && routed.ReroutesToBulk(NoSerialRows)
           && routed.ReroutesToBulk(SerialLimit(container));

    // The connection's configured MaxSerialInsert, or fewer where one command cannot carry that many rows of the container's
    // columns (the parameter limit). Rows beyond it go to the COPY, which has no such limit.
    private int SerialLimit(IStorageContainer container)
        => Math.Min(Configuration.MaxSerialInsert, PostgreSqlBatchInsertTranslator.RowsPerRequest(container));

    // The bulk route: a binary COPY of the planned rows. On any failure it returns a coded failure; it never reports Success
    // without actually writing the rows.
    private async Task<IGenericResult<T>> ExecuteBulk<T>(IDataCommand command, IDataContainer container, CancellationToken cancellationToken)
    {
        if (!string.Equals(command.CommandType, "BulkInsert", StringComparison.Ordinal))
        {
            return GenericResult<T>.Failure(DatabaseCommandLog.BulkRouteUnavailable(
                _logger, command.CommandType, _connectionName, "the PostgreSQL bulk mode copies bulk inserts only"));
        }

        var plan = PostgreSqlBulkInsertTranslator.Plan(command, container);
        if (!plan.IsSuccess || plan.Value is null)
            return plan.ToNewResult<T>();

        DatabaseCommandLog.BulkRouteChosen(_logger, command.CommandType, _connectionName, SerialLimit(container), string.Empty);
        try
        {
            if (_npgsqlConnection.State != ConnectionState.Open)
            {
                await _npgsqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
                PostgreSqlConnectionLog.ConnectionOpened(_logger, _connectionName);
            }

            PostgreSqlConnectionLog.BulkCopyStarting(_logger, _connectionName, plan.Value.Columns.Count);

            ulong rowsWritten;
            var importer = await _npgsqlConnection
                .BeginBinaryImportAsync(plan.Value.CopySql, cancellationToken).ConfigureAwait(false);
            await using (importer.ConfigureAwait(false))
            {
                var columnOrder = plan.Value.Columns.ToArray();
                foreach (var entity in plan.Value.Rows)
                {
                    await importer
                        .WriteRowAsync(cancellationToken, BuildRow(ExtractRowValues(entity), columnOrder))
                        .ConfigureAwait(false);
                }

                rowsWritten = await importer.CompleteAsync(cancellationToken).ConfigureAwait(false);
            }

            PostgreSqlConnectionLog.SqlCommandExecuted(_logger, plan.Value.CopySql, (int)rowsWritten);
            return GenericResult<T>.Success(ConvertScalarResult<T>((int)rowsWritten));
        }
        catch (Exception ex)
        {
            return GenericResult<T>.Failure(
                PostgreSqlConnectionLog.BulkCopyFailed(_logger, ex, _connectionName));
        }
    }

    // Why: build one COPY row in column order. DBNull.Value is how NpgsqlBinaryImporter.WriteRow encodes a
    // SQL NULL and keeps the array element non-null (no CS8625); WriteRow infers each column's PostgreSQL
    // type from the value's runtime type, so no per-column NpgsqlDbType table is needed.
    private static object[] BuildRow(IReadOnlyDictionary<string, object?> values, string[] columnOrder)
    {
        var row = new object[columnOrder.Length];
        for (var i = 0; i < columnOrder.Length; i++)
        {
            row[i] = values.TryGetValue(columnOrder[i], out var value) && value is not null
                ? value
                : DBNull.Value;
        }

        return row;
    }

    // Why: extract a row's column values as a name→value map without reflection. ETL bulk records arrive as
    // IDictionary<string,object?> (the transform's field-array output) and are read by key; POCO entities go
    // through their generated mapper's reflection-free MapToParameters.
    private IReadOnlyDictionary<string, object?> ExtractRowValues(object entity)
    {
        if (entity is IReadOnlyDictionary<string, object?> readOnly)
        {
            return readOnly;
        }

        if (entity is IDictionary<string, object?> dict)
        {
            // Why Ordinal: the generated MapToParameters (the other branch feeding BuildRow) keys by exact
            // SQL column name with StringComparer.Ordinal, and columnOrder is those same field names — so this
            // copy must match column names exactly the same way; a case mismatch is an upstream defect, not
            // something to silently absorb with OrdinalIgnoreCase.
            return new Dictionary<string, object?>(dict, StringComparer.Ordinal);
        }

        var mapper = PocoMapperCollection.ByName(entity.GetType().Name);
        if (mapper == PocoMapperCollection.NotFound)
        {
            throw new InvalidOperationException(
                PostgreSqlConnectionLog.NoMapperFound(
                    _logger, entity.GetType().Name, entity.GetType().FullName ?? entity.GetType().Name).Message);
        }

        return mapper.MapToParameters(entity);
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<global::System.Collections.Generic.IEnumerable<object>>> ExecuteRowsByType(
        NpgsqlCommand command,
        IStorageContainer container,
        global::System.Type elementType,
        CancellationToken cancellationToken)
    {
        PostgreSqlConnectionLog.TraceExecuteEntry(_logger, _connectionName);
        try
        {
            if (_npgsqlConnection.State != ConnectionState.Open)
            {
                await _npgsqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
                PostgreSqlConnectionLog.ConnectionOpened(_logger, _connectionName);
            }

            command.Connection = _npgsqlConnection;

            PostgreSqlConnectionLog.ExecutingSqlCommand(_logger, command.CommandText, command.Parameters.Count);

            using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            var rows = new List<object>();
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var rowResult = MapReaderRowToObject(reader, elementType, container);
                if (!rowResult.IsSuccess)
                {
                    return rowResult.ToNewResult<global::System.Collections.Generic.IEnumerable<object>>();
                }
                rows.Add(rowResult.Value!);
            }

            PostgreSqlConnectionLog.SqlCommandExecuted(_logger, command.CommandText, rows.Count);
            return GenericResult<global::System.Collections.Generic.IEnumerable<object>>.Success(rows);
        }
        catch (NpgsqlException ex)
        {
            return GenericResult<global::System.Collections.Generic.IEnumerable<object>>.Failure(
                ExceptionResultExtensions.FlattenException(ex).Prepend(PostgreSqlConnectionLog.QueryFailed(_logger, ex, _connectionName)));
        }
        catch (Exception ex)
        {
            return GenericResult<global::System.Collections.Generic.IEnumerable<object>>.Failure(
                PostgreSqlConnectionLog.SqlExecutionFailed(_logger, ex, command.CommandText));
        }
    }

    /// <summary>
    /// Opens the PostgreSQL connection.
    /// </summary>
    public async Task<IGenericResult> Connect(CancellationToken cancellationToken = default)
    {
        PostgreSqlConnectionLog.TraceConnectEntry(_logger, _connectionName);

        try
        {
            if (_npgsqlConnection.State != ConnectionState.Open)
            {
                await _npgsqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
                PostgreSqlConnectionLog.ConnectionOpened(_logger, _connectionName);
            }

            return GenericResult.Success();
        }
        catch (NpgsqlException ex)
        {
            return GenericResult.Failure(
                ExceptionResultExtensions.FlattenException(ex).Prepend(PostgreSqlConnectionLog.ConnectionFailed(_logger, ex, _connectionName)));
        }
        catch (TimeoutException ex)
        {
            return GenericResult.Failure(
                PostgreSqlConnectionLog.ConnectionFailed(_logger, ex, _connectionName));
        }
        catch (Exception ex)
        {
            // Why: OperationCanceledException, ObjectDisposedException, and other unexpected failures
            // must not propagate as unhandled exceptions — capture and fail gracefully.
            return GenericResult.Failure(
                PostgreSqlConnectionLog.ConnectionFailed(_logger, ex, _connectionName));
        }
    }

    /// <summary>
    /// Closes the PostgreSQL connection.
    /// </summary>
    public async Task<IGenericResult> Disconnect(CancellationToken cancellationToken = default)
    {
        PostgreSqlConnectionLog.TraceDisconnectEntry(_logger, _connectionName);

        try
        {
            if (_npgsqlConnection.State == ConnectionState.Open)
            {
                await _npgsqlConnection.CloseAsync().ConfigureAwait(false);
                PostgreSqlConnectionLog.ConnectionClosed(_logger, _connectionName);
            }

            return GenericResult.Success();
        }
        catch (NpgsqlException ex)
        {
            return GenericResult.Failure(
                ExceptionResultExtensions.FlattenException(ex).Prepend(PostgreSqlConnectionLog.ConnectionFailed(_logger, ex, _connectionName)));
        }
    }

    /// <summary>
    /// Tests the PostgreSQL connection by attempting to connect.
    /// </summary>
    public override Task<IGenericResult> TestConnection(CancellationToken cancellationToken = default)
    {
        return Connect(cancellationToken);
    }

    /// <summary>
    /// Performs a cheap liveness probe against PostgreSQL by executing <c>SELECT 1</c>
    /// on the long-lived connection (opening it first if not already open).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating whether the probe succeeded.</returns>
    public async Task<IGenericResult> Probe(CancellationToken cancellationToken = default)
    {
        PostgreSqlConnectionLog.TraceProbeEntry(_logger, _connectionName);

        try
        {
            if (_npgsqlConnection.State != ConnectionState.Open)
            {
                await _npgsqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
                PostgreSqlConnectionLog.ConnectionOpened(_logger, _connectionName);
            }

            var command = new NpgsqlCommand("SELECT 1", _npgsqlConnection);
            await using (command.ConfigureAwait(false))
            {
                await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            }

            PostgreSqlConnectionLog.ProbeSucceeded(_logger, _connectionName);
            return GenericResult.Success();
        }
        catch (NpgsqlException ex)
        {
            return GenericResult.Failure(
                ExceptionResultExtensions.FlattenException(ex).Prepend(PostgreSqlConnectionLog.QueryFailed(_logger, ex, _connectionName)));
        }
        catch (Exception ex)
        {
            return GenericResult.Failure(
                PostgreSqlConnectionLog.SqlExecutionFailed(_logger, ex, "SELECT 1"));
        }
    }

    /// <summary>
    /// Disposes the PostgreSQL connection.
    /// </summary>
    public override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _npgsqlConnection?.Dispose();
        }
        base.Dispose(disposing);
    }

    // -----------------------------------------------------------------------
    // Reader-to-type materialization — mirrors MsSqlConnection, uses DbDataReader
    // -----------------------------------------------------------------------

    private async Task<IGenericResult<T>> MapReaderToType<T>(DbDataReader reader, IStorageContainer container, CancellationToken cancellationToken)
    {
        var targetType = typeof(T);

        if (IsCollectionType(targetType, out var itemType))
        {
            if (IsDataRowType(itemType!))
                return GenericResult<T>.Success((T)ConvertToCollectionType(
                    await ReadDataRows(reader, cancellationToken).ConfigureAwait(false), targetType, itemType!));

            var listResult = CreateElementList(itemType!);
            if (!listResult.IsSuccess)
                return listResult.ToNewResult<T>();

            var list = listResult.Value!;
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var itemResult = MapReaderRowToObject(reader, itemType!, container);
                if (!itemResult.IsSuccess)
                    return itemResult.ToNewResult<T>();
                list.Add(itemResult.Value);
            }

            return GenericResult<T>.Success((T)ConvertToCollectionType(list, targetType, itemType!));
        }

        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var itemResult = MapReaderRowToObject(reader, targetType, container);
            return itemResult.IsSuccess ? GenericResult<T>.Success((T)itemResult.Value!) : itemResult.ToNewResult<T>();
        }

        return GenericResult<T>.Success(default!);
    }

    // Why: reflection-free element-list construction — same pattern as MsSqlConnection.
    // Dictionary<string, object?> rows are a special case with no [GenerateMapper]; everything
    // else must have a generated mapper or fails loud.
    private IGenericResult<System.Collections.IList> CreateElementList(Type itemType)
    {
        var mapper = PocoMapperCollection.ByName(itemType.Name);
        if (mapper != PocoMapperCollection.NotFound)
            return GenericResult<System.Collections.IList>.Success(mapper.CreateList());

        if (itemType.IsGenericType && itemType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            return GenericResult<System.Collections.IList>.Success(new List<Dictionary<string, object?>>());

        var message = PostgreSqlConnectionLog.NoMapperFound(_logger, itemType.Name, itemType.FullName ?? itemType.Name);
        return GenericResult<System.Collections.IList>.Failure(message);
    }

    private IGenericResult<object> MapReaderRowToObject(DbDataReader reader, Type targetType, IStorageContainer container)
    {
        // Handle primitive types (single column, direct mapping)
        if (targetType.IsPrimitive || targetType == typeof(string) || targetType == typeof(DateTime) ||
            targetType == typeof(decimal) || targetType == typeof(Guid))
        {
            return GenericResult<object>.Success(ConvertValue(reader.GetValue(0), targetType));
        }

        // Handle Dictionary<string, object?> (used by data preview endpoints)
        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            var dict = new Dictionary<string, object?>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < reader.FieldCount; i++)
                dict[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            return GenericResult<object>.Success(dict);
        }

        // Try generated POCO mapper (required for complex types)
        var mapper = PocoMapperCollection.ByName(targetType.Name);
        if (mapper == PocoMapperCollection.NotFound)
        {
            var message = PostgreSqlConnectionLog.NoMapperFound(_logger, targetType.Name, targetType.FullName ?? targetType.Name);
            return GenericResult<object>.Failure(message);
        }

        var mappingResult = mapper.MapFromReader(reader, container);
        if (!mappingResult.IsSuccess)
        {
            // Why: mapper already logged the detailed column-level failure; this log adds the
            // type-level context. No errorMessage parameter needed — the mapper's message is
            // already in the log output from the generated mapper code.
            var message = PostgreSqlConnectionLog.MappingFailed(_logger, targetType.Name);
            return GenericResult<object>.Failure(message);
        }

        return GenericResult<object>.Success(mappingResult.Value!);
    }

    private static object ConvertValue(object value, Type targetType)
    {
        var underlyingType = Nullable.GetUnderlyingType(targetType);

        if (value == null || value == DBNull.Value)
        {
            // Why: NULL is valid only for a reference type or Nullable<T>. Mapping NULL into a
            // non-nullable value type would silently fabricate default(T) — fail loud instead
            // (NO-FALLBACKS rule); the read path's try/catch turns this into a structured Failure.
            if (!targetType.IsValueType || underlyingType is not null)
                return null!;

            throw new InvalidOperationException(
                $"Cannot map a NULL database value to non-nullable type '{targetType.Name}'.");
        }

        var effectiveType = underlyingType ?? targetType;

        if (effectiveType.IsInstanceOfType(value))
            return value;

        return Convert.ChangeType(value, effectiveType, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static T ConvertScalarResult<T>(int value)
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


    // Why: IDataRow is the framework's row — a query whose columns are known only at runtime (a DataSet
    // query, a preview) asks for it, and it needs no generated mapper because it carries its own schema.
    private static bool IsDataRowType(Type t) => t == typeof(global::Fdw.Data.DataContainers.Abstractions.IDataRow) || t == typeof(global::Fdw.Data.DataContainers.Abstractions.DataRow);

    // Why the schema is built once and shared: every row of a result set has the same columns, and
    // DataRow addresses its values positionally against that schema, so building one per row would be
    // both wasteful and a chance for the two to disagree. Column names and CLR types come from the
    // reader itself, which is the only thing that knows the shape of an ad-hoc result set.
    private static async Task<System.Collections.IList> ReadDataRows(
        DbDataReader reader, CancellationToken cancellationToken)
    {
        var fields = new ISchemaField[reader.FieldCount];
        for (var i = 0; i < reader.FieldCount; i++)
            fields[i] = new global::Fdw.Data.DataContainers.Abstractions.SchemaField(reader.GetName(i), reader.GetFieldType(i), i);

        var schema = global::Fdw.Data.DataContainers.Abstractions.DataSchema.FromFields(fields);
        var rows = new List<global::Fdw.Data.DataContainers.Abstractions.IDataRow>();

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var values = new object?[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; i++)
                values[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);

            rows.Add(new global::Fdw.Data.DataContainers.Abstractions.DataRow(schema, values));
        }

        return rows;
    }

    private static bool IsCollectionType(Type type, out Type? itemType)
    {
        if (type.IsArray)
        {
            itemType = type.GetElementType();
            return true;
        }

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

    private static object ConvertToCollectionType(System.Collections.IList list, Type targetType, Type itemType)
    {
        if (targetType.IsArray)
        {
            var array = Array.CreateInstance(itemType, list.Count);
            list.CopyTo(array, 0);
            return array;
        }

        // Why: the generated mapper's CreateList() returns the concrete List<T> which satisfies
        // IEnumerable<T>, IList<T>, and IReadOnlyList<T>. Cast covers all standard collection interfaces.
        return list;
    }
    /// <inheritdoc />
    /// <remarks>
    /// The streaming counterpart of <c>Execute</c>: same translation, but the reader is handed back
    /// as a cursor instead of being materialised into a list.
    /// </remarks>
    public async Task<IGenericResult<IRecordSource<DataRecord>>> OpenRecordSource(
        IDataCommand command, IDataContainer container, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(container);

        var translationResult = await GetTranslator(command.CommandType)
            .Translate(command, container, cancellationToken).ConfigureAwait(false);
        if (!translationResult.IsSuccess || translationResult.Value is null)
        {
            return translationResult.ToNewResult<IRecordSource<DataRecord>>();
        }

        var npgsqlCommand = translationResult.Value;
        NpgsqlDataReader? reader = null;
        try
        {
            if (_npgsqlConnection.State != System.Data.ConnectionState.Open)
            {
                await _npgsqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
            }

            npgsqlCommand.Connection = _npgsqlConnection;
            PostgreSqlConnectionLog.ExecutingSqlCommand(_logger, npgsqlCommand.CommandText, npgsqlCommand.Parameters.Count);
            reader = await npgsqlCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            var fields = container.Schema.Fields.OfType<Fdw.Data.Abstractions.IDataField>().ToList();
            var cursor = new CursorRecordSource(new DataReaderRowSource(reader), fields, _logger);
            var scoped = new PostgreSqlRecordSource(cursor, npgsqlCommand, reader);
            reader = null;
            return GenericResult<IRecordSource<DataRecord>>.Success(scoped);
        }
        catch (NpgsqlException ex)
        {
            if (reader is not null) await reader.DisposeAsync().ConfigureAwait(false);
            return GenericResult<IRecordSource<DataRecord>>.Failure(
                ExceptionResultExtensions.FlattenException(ex).Prepend(PostgreSqlConnectionLog.SqlExecutionFailed(_logger, ex, npgsqlCommand.CommandText)));
        }
    }
}
