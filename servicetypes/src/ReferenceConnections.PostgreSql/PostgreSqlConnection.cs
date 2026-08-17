using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Data.PostgreSql;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.PostgreSql.Logging;
using Fdw.Services.Connections.PostgreSql.Results;
using Microsoft.Extensions.Logging;
using Npgsql;

using Fdw.Services.Connections.PostgreSql;

using Fdw.Services.Connections.PostgreSql.Discovery;

using Fdw.Services.Connections.PostgreSql.Authentication;

using Fdw.Services.Connections.PostgreSql.Commands;

using Fdw.Services.Connections.PostgreSql.Validation;


namespace ReferenceConnections.PostgreSql;

/// <summary>
/// PostgreSQL data connection.
/// Holds a single long-lived <see cref="NpgsqlConnection"/> that is opened on first use.
/// </summary>
[ExcludeFromCodeCoverage] // Excluded: requires PostgreSQL connection
public sealed class PostgreSqlConnection : ConnectionBase<NpgsqlCommand, PostgreSqlConnectionConfiguration, PostgreSqlConnection>, ISupportsHealthProbe
{
    private readonly NpgsqlConnection _npgsqlConnection;
    private readonly ILogger<PostgreSqlConnection> _logger;
    // Why: After config-split, Name lives on the parent ConnectionConfiguration header, not on the typed body.
    // The factory extracts it and passes it explicitly so logging uses the correct connection name.
    private readonly string _connectionName;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlConnection"/> class.
    /// </summary>
    /// <param name="logger">Logger for connection operations.</param>
    /// <param name="configuration">The typed PostgreSQL connection configuration.</param>
    /// <param name="npgsqlConnection">The underlying Npgsql connection.</param>
    /// <param name="connectionName">The connection name from the parent ConnectionConfiguration header.</param>
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
    /// Gets a value indicating whether this connection is stale and should be recreated.
    /// A connection is stale when the underlying NpgsqlConnection has been disposed and its ConnectionString cleared.
    /// </summary>
    public override bool IsStale => string.IsNullOrEmpty(_npgsqlConnection.ConnectionString);

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

            // Why: the BulkInsert translator emits a marker command (CommandType.StoredProcedure + a
            // "-- BULK INSERT MARKER" comment) carrying the real COPY SQL and the row data in parameters.
            // Detect it and run the binary COPY here; otherwise the marker comment falls through to
            // ExecuteNonQuery as a no-op and reports Success while inserting nothing. Mirrors MsSqlConnection.
            if (command.CommandType == CommandType.StoredProcedure &&
                command.CommandText.StartsWith("-- BULK INSERT MARKER", StringComparison.Ordinal))
            {
                return await ExecuteBulkCopy<T>(command, cancellationToken).ConfigureAwait(false);
            }

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

            PostgreSqlConnectionLog.SqlCommandExecuted(_logger, command.CommandText, 0);

            return GenericResult<T>.Success(mappedResult);
        }
        catch (NpgsqlException ex)
        {
            return GenericResult<T>.Failure(
                PostgreSqlConnectionLog.QueryFailed(_logger, ex, _connectionName));
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
    /// Executes a PostgreSQL binary COPY from the marker command produced by the BulkInsert translator
    /// (<see cref="PostgreSqlBulkInsertTranslator"/>). The marker carries the real
    /// <c>COPY … FROM STDIN BINARY</c> SQL and the row data in its parameters; this streams the rows
    /// through an <see cref="NpgsqlBinaryImporter"/>. On any failure it returns a coded failure — it
    /// never reports Success without actually writing the rows.
    /// </summary>
    private async Task<IGenericResult<T>> ExecuteBulkCopy<T>(
        NpgsqlCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            // Why: these parameters are always set by the BulkInsert translator; a missing one is an
            // upstream contract break — fail loud rather than silently import zero rows.
            if (command.Parameters["__BulkCopy_CopySql"]?.Value is not string copySql || copySql.Length == 0)
            {
                return GenericResult<T>.Failure(
                    PostgreSqlConnectionLog.BulkCopyMetadataMissing(_logger, _connectionName, "__BulkCopy_CopySql"));
            }

            if (command.Parameters["__BulkCopy_ColumnMappings"]?.Value is not string columnCsv || columnCsv.Length == 0)
            {
                return GenericResult<T>.Failure(
                    PostgreSqlConnectionLog.BulkCopyMetadataMissing(_logger, _connectionName, "__BulkCopy_ColumnMappings"));
            }

            if (command.Parameters["__BulkCopy_Entities"]?.Value is not IEnumerable entities)
            {
                return GenericResult<T>.Failure(
                    PostgreSqlConnectionLog.BulkCopyMetadataMissing(_logger, _connectionName, "__BulkCopy_Entities"));
            }

            var columnOrder = columnCsv.Split(',', StringSplitOptions.RemoveEmptyEntries);
            PostgreSqlConnectionLog.BulkCopyStarting(_logger, _connectionName, columnOrder.Length);

            ulong rowsWritten;
            var importer = await _npgsqlConnection
                .BeginBinaryImportAsync(copySql, cancellationToken).ConfigureAwait(false);
            await using (importer.ConfigureAwait(false))
            {
                foreach (var entity in entities)
                {
                    await importer
                        .WriteRowAsync(cancellationToken, BuildRow(ExtractRowValues(entity), columnOrder))
                        .ConfigureAwait(false);
                }

                rowsWritten = await importer.CompleteAsync(cancellationToken).ConfigureAwait(false);
            }

            PostgreSqlConnectionLog.SqlCommandExecuted(_logger, copySql, (int)rowsWritten);
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
                rows.Add(MapReaderRowToObject(reader, elementType, container));
            }

            PostgreSqlConnectionLog.SqlCommandExecuted(_logger, command.CommandText, rows.Count);
            return GenericResult<global::System.Collections.Generic.IEnumerable<object>>.Success(rows);
        }
        catch (NpgsqlException ex)
        {
            return GenericResult<global::System.Collections.Generic.IEnumerable<object>>.Failure(
                PostgreSqlConnectionLog.QueryFailed(_logger, ex, _connectionName));
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
                PostgreSqlConnectionLog.ConnectionFailed(_logger, ex, _connectionName));
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
                PostgreSqlConnectionLog.ConnectionFailed(_logger, ex, _connectionName));
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
                PostgreSqlConnectionLog.QueryFailed(_logger, ex, _connectionName));
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

    private async Task<T> MapReaderToType<T>(DbDataReader reader, IStorageContainer container, CancellationToken cancellationToken)
    {
        var targetType = typeof(T);

        if (IsCollectionType(targetType, out var itemType))
        {
            if (IsDataRowType(itemType!))
                return (T)ConvertToCollectionType(
                    await ReadDataRows(reader, cancellationToken).ConfigureAwait(false), targetType, itemType!);

            var list = CreateElementList(itemType!);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                list.Add(MapReaderRowToObject(reader, itemType!, container));
            return (T)ConvertToCollectionType(list, targetType, itemType!);
        }

        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            return (T)MapReaderRowToObject(reader, targetType, container);

        return default!;
    }

    // Why: reflection-free element-list construction — same pattern as MsSqlConnection.
    // Dictionary<string, object?> rows are a special case with no [GenerateMapper]; everything
    // else must have a generated mapper or fails loud.
    private System.Collections.IList CreateElementList(Type itemType)
    {
        var mapper = PocoMapperCollection.ByName(itemType.Name);
        if (mapper != PocoMapperCollection.NotFound)
            return mapper.CreateList();

        if (itemType.IsGenericType && itemType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            return new List<Dictionary<string, object?>>();

        var message = PostgreSqlConnectionLog.NoMapperFound(_logger, itemType.Name, itemType.FullName ?? itemType.Name);
        throw new InvalidOperationException(message.Message);
    }

    private object MapReaderRowToObject(DbDataReader reader, Type targetType, IStorageContainer container)
    {
        // Handle primitive types (single column, direct mapping)
        if (targetType.IsPrimitive || targetType == typeof(string) || targetType == typeof(DateTime) ||
            targetType == typeof(decimal) || targetType == typeof(Guid))
        {
            return ConvertValue(reader.GetValue(0), targetType);
        }

        // Handle Dictionary<string, object?> (used by data preview endpoints)
        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            var dict = new Dictionary<string, object?>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < reader.FieldCount; i++)
                dict[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            return dict;
        }

        // Try generated POCO mapper (required for complex types)
        var mapper = PocoMapperCollection.ByName(targetType.Name);
        if (mapper == PocoMapperCollection.NotFound)
        {
            var message = PostgreSqlConnectionLog.NoMapperFound(_logger, targetType.Name, targetType.FullName ?? targetType.Name);
            throw new InvalidOperationException(message.Message);
        }

        var mappingResult = mapper.MapFromReader(reader, container);
        if (!mappingResult.IsSuccess)
        {
            // Why: mapper already logged the detailed column-level failure; this log adds the
            // type-level context. No errorMessage parameter needed — the mapper's message is
            // already in the log output from the generated mapper code.
            throw new InvalidOperationException(
                PostgreSqlConnectionLog.MappingFailed(_logger, targetType.Name).Message);
        }

        return mappingResult.Value!;
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
}
