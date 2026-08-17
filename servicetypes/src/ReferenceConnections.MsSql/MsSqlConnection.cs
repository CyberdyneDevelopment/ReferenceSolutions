using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.MsSql;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.RowSources.DataReader.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.MsSql.ErrorHandlers;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.MsSql.Logging;
using ReferenceConnections.MsSql.Mapping;
using Fdw.Services.Connections.MsSql.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

using Fdw.Services.Connections.MsSql;

using Fdw.Services.Connections.MsSql.Discovery;

using Fdw.Services.Connections.MsSql.Authentication;

using Fdw.Services.Connections.MsSql.Limits;

using Fdw.Services.Connections.MsSql.Messages;

using Fdw.Services.Connections.MsSql.Commands;

using Fdw.Services.Connections.MsSql.Validation;



namespace ReferenceConnections.MsSql;

/// <summary>
/// SQL Server data connection.
/// Inherits from ConnectionBase which handles IDataCommand → SqlCommand translation.
/// Uses ADO.NET connection pooling: opens a new SqlConnection per operation from the
/// driver's internal pool, rather than holding a single connection open.
/// </summary>
[ExcludeFromCodeCoverage] // Excluded: requires SQL Server connection
public sealed class MsSqlConnection : ConnectionBase<SqlCommand, MsSqlConnectionConfiguration, MsSqlConnection>, ITransactionalDataConnection, IRecordSourceConnection, ISupportsHealthProbe, IPooledConnectionSource
{
    private readonly string _connectionString;
    private readonly string? _accessToken;
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
    /// <param name="accessToken">Optional access token for token-based authentication.</param>
    /// <param name="authenticationContextAccessor">
    /// Optional ambient accessor the connection reads the calling principal from at plan time, for
    /// RLS SESSION_CONTEXT injection.
    /// </param>
    public MsSqlConnection(
        ILogger<MsSqlConnection> logger,
        MsSqlConnectionConfiguration configuration,
        string connectionString,
        string? accessToken = null,
        IAuthenticationContextAccessor? authenticationContextAccessor = null)
        : base(logger, configuration)
    {
        _connectionString = connectionString;
        _accessToken = accessToken;
        _logger = logger;
        _authenticationContextAccessor = authenticationContextAccessor;
    }

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

    /// <summary>
    /// Gets a value indicating whether this connection is stale and should be recreated.
    /// With pooled connections, staleness is not applicable — each operation gets a fresh
    /// connection from the ADO.NET pool.
    /// </summary>
    public override bool IsStale => false;

    /// <summary>
    /// Gets the connection string for creating pooled connections.
    /// Internal to allow schema discovery and commands within this assembly.
    /// </summary>
    internal string ConnectionString => _connectionString;

    /// <summary>
    /// Gets the access token used for token-based authentication (e.g., AzureCli mode).
    /// Null when connection string authentication is used instead.
    /// </summary>
    internal string? AccessToken => _accessToken;

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
        MsSqlConnectionLogger.TraceExecuteEntry(Logger, command.CommandText);

        try
        {
            // Open a pooled connection for this operation
            var sqlConnection = CreatePooledConnection();
            await using (sqlConnection.ConfigureAwait(false))
            {
                await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
                await SetUserSessionContext(sqlConnection, cancellationToken).ConfigureAwait(false);

                // Assign the connection to the command
                command.Connection = sqlConnection;

                // ⭐ Detect SqlBulkCopy marker
                if (command.CommandType == CommandType.StoredProcedure &&
                    command.CommandText.StartsWith("-- BULK INSERT MARKER", StringComparison.Ordinal))
                {
                    return await ExecuteBulkCopy<T>(command, sqlConnection, cancellationToken).ConfigureAwait(false);
                }

                MsSqlConnectionLogger.ExecutingSqlCommand(Logger, command.CommandText, command.Parameters.Count);

                // Determine if this is a query (SELECT) or command (INSERT/UPDATE/DELETE)
                var isQuery = command.CommandText.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase);

                if (!isQuery)
                {
                    // For non-query commands, execute and return rows affected
                    var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    MsSqlConnectionLogger.SqlCommandExecuted(Logger, command.CommandText, rowsAffected);

                    // Return rowsAffected as T if possible, otherwise default
                    var result = ConvertScalarResult<T>(rowsAffected);
                    return GenericResult<T>.Success(result);
                }

                // For SELECT queries, use reader and map results
                using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                var mappedResult = await MapReaderToType<T>(reader, container, cancellationToken).ConfigureAwait(false);

                MsSqlConnectionLogger.SqlCommandExecuted(Logger, command.CommandText, 0);

                return GenericResult<T>.Success(mappedResult);
            }
        }
        catch (SqlException ex)
        {
            MsSqlConnectionLogger.SqlExecutionError(Logger, command.CommandText, ex.Message, ex.Number);

            var handler = SqlErrorHandlers.ByErrorNumber(ex.Number);
            return GenericResult<T>.Failure(handler.CreateFailureMessage(_logger, ex, command.CommandText));
        }
        catch (Exception ex)
        {
            MsSqlConnectionLogger.ExecutionException(Logger, command.CommandText, ex.Message);
            return GenericResult<T>.Failure(MsSqlConnectionLogger.SqlExecutionFailedWithMessage(_logger, ex, command.CommandText));
        }
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
        SqlConnection? sqlConnection = null;
        SqlDataReader? reader = null;
        try
        {
            sqlConnection = CreatePooledConnection();
            await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await SetUserSessionContext(sqlConnection, cancellationToken).ConfigureAwait(false);
            sqlCommand.Connection = sqlConnection;

            MsSqlConnectionLogger.ExecutingSqlCommand(Logger, sqlCommand.CommandText, sqlCommand.Parameters.Count);
            reader = await sqlCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

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
            MsSqlConnectionLogger.SqlExecutionError(Logger, sqlCommand.CommandText, ex.Message, ex.Number);
            var handler = SqlErrorHandlers.ByErrorNumber(ex.Number);
            return GenericResult<IRecordSource<DataRecord>>.Failure(handler.CreateFailureMessage(_logger, ex, sqlCommand.CommandText));
        }
        catch (Exception ex)
        {
            if (reader is not null) await reader.DisposeAsync().ConfigureAwait(false);
            if (sqlConnection is not null) await sqlConnection.DisposeAsync().ConfigureAwait(false);
            return GenericResult<IRecordSource<DataRecord>>.Failure(MsSqlConnectionLogger.SqlExecutionFailedWithMessage(_logger, ex, sqlCommand.CommandText));
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
        MsSqlConnectionLogger.TraceExecuteEntry(Logger, command.CommandText);
        try
        {
            // Open a pooled connection for this operation (sets RLS SESSION_CONTEXT, like Execute<T>).
            var sqlConnection = CreatePooledConnection();
            await using (sqlConnection.ConfigureAwait(false))
            {
                await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
                await SetUserSessionContext(sqlConnection, cancellationToken).ConfigureAwait(false);
                command.Connection = sqlConnection;

                MsSqlConnectionLogger.ExecutingSqlCommand(Logger, command.CommandText, command.Parameters.Count);

                using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                var rows = new List<object>();
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    // Why: per-row materialization is already Type-based (resolves the element's
                    // generated mapper by name) — no MakeGenericMethod / Activator.
                    rows.Add(MapReaderRowToObject(reader, elementType, container, Logger));
                }
                return GenericResult<global::System.Collections.Generic.IEnumerable<object>>.Success(rows);
            }
        }
        catch (SqlException ex)
        {
            MsSqlConnectionLogger.SqlExecutionError(Logger, command.CommandText, ex.Message, ex.Number);
            var handler = SqlErrorHandlers.ByErrorNumber(ex.Number);
            return GenericResult<global::System.Collections.Generic.IEnumerable<object>>.Failure(
                handler.CreateFailureMessage(_logger, ex, command.CommandText));
        }
        catch (Exception ex)
        {
            MsSqlConnectionLogger.ExecutionException(Logger, command.CommandText, ex.Message);
            return GenericResult<global::System.Collections.Generic.IEnumerable<object>>.Failure(
                MsSqlConnectionLogger.SqlExecutionFailedWithMessage(_logger, ex, command.CommandText));
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
    internal static Task<T> MapReaderToTypeInternal<T>(
        SqlDataReader reader, IStorageContainer container, ILogger logger, CancellationToken cancellationToken)
    {
        var targetType = typeof(T);
        if (IsCollectionType(targetType, out var itemType))
        {
            return MapReaderToTypeCollection<T>(reader, container, itemType!, logger, cancellationToken);
        }
        return MapReaderSingle<T>(reader, container, logger, cancellationToken);
    }

    private static async Task<T> MapReaderToTypeCollection<T>(
        SqlDataReader reader, IStorageContainer container, Type itemType, ILogger logger, CancellationToken cancellationToken)
    {
        if (IsDataRowType(itemType))
            return (T)ConvertToCollectionType(
                await ReadDataRows(reader, cancellationToken).ConfigureAwait(false), typeof(T), itemType);

        var list = CreateElementList(itemType, logger);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            list.Add(MapReaderRowToObject(reader, itemType, container, logger));
        return (T)ConvertToCollectionType(list, typeof(T), itemType);
    }

    // Why: IDataRow is the framework's row — a query whose columns are known only at runtime (a DataSet
    // query, a preview) asks for it, and it needs no generated mapper because it carries its own schema.
    private static bool IsDataRowType(Type t) => t == typeof(global::Fdw.Data.DataContainers.Abstractions.IDataRow) || t == typeof(global::Fdw.Data.DataContainers.Abstractions.DataRow);

    // Why the schema is built once and shared: every row of a result set has the same columns, and
    // DataRow addresses its values positionally against that schema, so building one per row would be
    // both wasteful and a chance for the two to disagree. Column names and CLR types come from the
    // reader itself, which is the only thing that knows the shape of an ad-hoc result set.
    private static async Task<System.Collections.IList> ReadDataRows(
        SqlDataReader reader, CancellationToken cancellationToken)
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

    // Why: reflection-free element-list construction (replaces Activator.CreateInstance(List<>.MakeGenericType)).
    // Mapped POCO element types use their generated mapper's CreateList(); the only other element type that
    // reaches here is Dictionary<string, object?> (data-preview rows), which has no [GenerateMapper] and is
    // constructed directly. Any other unmapped element type fails loud — there is no reflection fallback.
    private static System.Collections.IList CreateElementList(Type itemType, ILogger logger)
    {
        var mapper = PocoMapperCollection.ByName(itemType.Name);
        if (mapper != PocoMapperCollection.NotFound)
            return mapper.CreateList();

        if (itemType.IsGenericType && itemType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            return new List<Dictionary<string, object?>>();

        var message = MsSqlConnectionLogger.NoMapperFound(logger, itemType.Name, itemType.FullName ?? itemType.Name);
        throw new InvalidOperationException(message.Message);
    }

    private static async Task<T> MapReaderSingle<T>(
        SqlDataReader reader, IStorageContainer container, ILogger logger, CancellationToken cancellationToken)
    {
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            return (T)MapReaderRowToObject(reader, typeof(T), container, logger);
        return default!;
    }

    /// <summary>
    /// Maps SqlDataReader results to type T using container schema and converters.
    /// </summary>
    private async Task<T> MapReaderToType<T>(SqlDataReader reader, IStorageContainer container, CancellationToken cancellationToken)
    {
        var targetType = typeof(T);

        // Handle collection types (IEnumerable<TItem>, List<TItem>, TItem[], etc.)
        if (IsCollectionType(targetType, out var itemType))
        {
            if (IsDataRowType(itemType!))
                return (T)ConvertToCollectionType(
                    await ReadDataRows(reader, cancellationToken).ConfigureAwait(false), targetType, itemType!);

            var list = CreateElementList(itemType!, _logger);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var item = MapReaderRowToObject(reader, itemType!, container, _logger);
                list.Add(item);
            }

            // Convert to requested collection type
            return (T)ConvertToCollectionType(list, targetType, itemType!);
        }

        // Handle single object
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return (T)MapReaderRowToObject(reader, targetType, container, _logger);
        }

        // No rows, return default
        return default!;
    }

    /// <summary>
    /// Maps a single SqlDataReader row to an object.
    /// Uses generated POCO mappers - NO REFLECTION FALLBACK.
    /// </summary>
    private static object MapReaderRowToObject(
        SqlDataReader reader,
        Type targetType,
        IStorageContainer container,
        ILogger logger)
    {
        // Handle primitive types (single column, direct mapping)
        if (targetType.IsPrimitive || targetType == typeof(string) || targetType == typeof(DateTime) ||
            targetType == typeof(decimal) || targetType == typeof(Guid))
        {
            return ConvertValue(reader.GetValue(0), targetType);
        }

        // Handle Dictionary<string, object?> (used by data preview endpoints).
        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            var dict = new Dictionary<string, object?>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < reader.FieldCount; i++)
            {
                dict[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }

            return dict;
        }

        // Try generated POCO mapper (required for complex types)
        // ByName looks up by the short type name (e.g., "DataSetEntity")
        // Note: ByName returns NotFound sentinel if not found, NOT null
        var mapper = PocoMapperCollection.ByName(targetType.Name);
        if (mapper == PocoMapperCollection.NotFound)
        {
            var message = MsSqlConnectionLogger.NoMapperFound(logger, targetType.Name, targetType.FullName ?? targetType.Name);
            throw new InvalidOperationException(message.Message);
        }

        var mappingResult = mapper.MapFromReader(reader, container);
        if (!mappingResult.IsSuccess)
        {
            var message = MsSqlConnectionLogger.MappingFailed(logger, targetType.Name, mappingResult.CurrentMessage ?? "Unknown error");
            throw new InvalidOperationException(message.Message);
        }

        return mappingResult.Value!;
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
    /// <returns>A dictionary containing the row values.</returns>
    internal static Dictionary<string, object?> MapReaderRowToDictionary(SqlDataReader reader, RowMappingContext ctx)
    {
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

            var dbValue = reader.GetValue(ordinal);
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
    /// Executes SqlBulkCopy operation from metadata in SqlCommand.
    /// </summary>
    private async Task<IGenericResult<T>> ExecuteBulkCopy<T>(
        SqlCommand command,
        SqlConnection sqlConnection,
        CancellationToken cancellationToken)
    {
        try
        {
            // Extract metadata from parameters
            var dataTableParam = command.Parameters["@__BulkCopy_DataTable"];
            var destinationParam = command.Parameters["@__BulkCopy_Destination"];
            var columnMappingsParam = command.Parameters["@__BulkCopy_ColumnMappings"];

            if (dataTableParam?.Value is not DataTable dataTable)
            {
                return GenericResult<T>.Failure(MsSqlConnectionResultCodes.ByName("InvalidBulkCopyDataTable"));
            }

            var destination = destinationParam?.Value as string ?? throw new InvalidOperationException("Missing destination");
            var columnMappings = (columnMappingsParam?.Value as string ?? "").Split(',');

            MsSqlConnectionLogger.ExecutingSqlCommand(Logger, $"BULK INSERT INTO {destination}", dataTable.Rows.Count);

            // Configure SqlBulkCopy with safe defaults
            var options = SqlBulkCopyOptions.CheckConstraints |
                         SqlBulkCopyOptions.FireTriggers |
                         SqlBulkCopyOptions.KeepIdentity;
            // Note: NO TableLock for better concurrency

            using var bulkCopy = new SqlBulkCopy(sqlConnection, options, null)
            {
                DestinationTableName = destination,
                BatchSize = 1000,
                BulkCopyTimeout = 300 // 5 minutes
            };

            // Map columns
            foreach (var columnName in columnMappings)
            {
                if (!string.IsNullOrWhiteSpace(columnName))
                {
                    bulkCopy.ColumnMappings.Add(columnName, columnName);
                }
            }

            // Execute bulk copy
            await bulkCopy.WriteToServerAsync(dataTable, cancellationToken).ConfigureAwait(false);

            // Return row count
            var rowCount = dataTable.Rows.Count;
            MsSqlConnectionLogger.SqlCommandExecuted(Logger, $"BULK INSERT INTO {destination}", rowCount);

            var result = ConvertScalarResult<T>(rowCount);
            return GenericResult<T>.Success(result);
        }
        catch (Exception ex)
        {
            MsSqlConnectionLogger.ExecutionException(Logger, "BULK INSERT", ex.Message);
            return GenericResult<T>.Failure(MsSqlConnectionLogger.BulkCopyFailed(_logger, ex));
        }
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
                await SetUserSessionContext(sqlConnection, cancellationToken).ConfigureAwait(false);
                MsSqlConnectionLogger.TraceConnectionOpened(Logger, sqlConnection.DataSource, sqlConnection.Database);
                MsSqlConnectionLogger.ConnectionOpened(Logger, ConnectionType);
                return GenericResult.Success();
            }
        }
        catch (SqlException ex)
        {
            MsSqlConnectionLogger.ConnectionFailed(Logger, ConnectionType, ex.Message);

            var handler = SqlErrorHandlers.ByErrorNumber(ex.Number);
            return GenericResult.Failure(handler.CreateFailureMessage(_logger, ex, $"CONNECT {Configuration.Server}/{Configuration.Database}"));
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
        var connectionName = ((IGenericConfiguration)Configuration).Name;
        MsSqlConnectionLogger.TraceProbeEntry(Logger, connectionName);

        try
        {
            var sqlConnection = CreatePooledConnection();
            await using (sqlConnection.ConfigureAwait(false))
            {
                await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
                await SetUserSessionContext(sqlConnection, cancellationToken).ConfigureAwait(false);

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
            var handler = SqlErrorHandlers.ByErrorNumber(ex.Number);
            return GenericResult.Failure(handler.CreateFailureMessage(_logger, ex, "SELECT 1"));
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
        var connectionName = ((IGenericConfiguration)Configuration).Name;
        MsSqlConnectionLogger.TransactionStarted(_logger, connectionName);
        try
        {
            var sqlConnection = CreatePooledConnection();
            await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await SetUserSessionContext(sqlConnection, cancellationToken).ConfigureAwait(false);
            var transaction = await sqlConnection.BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false) as SqlTransaction;
            // Why: BeginTransactionAsync on SqlConnection always returns SqlTransaction;
            // the cast is safe and avoids an interface indirection in the hot path.
            return GenericResult<IDataConnectionTransaction>.Success(
                new MsSqlDataConnectionTransaction(sqlConnection, transaction!, _logger, connectionName));
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
    internal Task SetUserSessionContext(SqlConnection connection, CancellationToken cancellationToken)
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
            var rows = await QueryRowsInternal(sql, cancellationToken).ConfigureAwait(false);
            return GenericResult<List<Dictionary<string, object?>>>.Success(rows);
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
            var handler = SqlErrorHandlers.ByErrorNumber(ex.Number);
            return GenericResult<List<Dictionary<string, object?>>>.Failure(
                handler.CreateFailureMessage(_logger, ex, sql));
        }
    }

    /// <summary>
    /// Queries a child configuration table with a LEFT JOIN to its parent table.
    /// Both tables must be in the same schema. Returns paired child/parent rows.
    /// Used by the configuration provider for parent-child table loading during startup.
    /// </summary>
    /// <param name="schema">The database schema for both tables.</param>
    /// <param name="childTableName">The child table name.</param>
    /// <param name="parentTableName">The parent table name.</param>
    /// <param name="foreignKeyColumn">The FK column on the child table referencing parent.Id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing (child row, parent row) tuples.
    /// Returns an empty list (success) if the table does not exist.</returns>
    // Why: Cross-schema joins are not needed because ctrl and cfg are independent data
    // sets — one schema value covers the entire JOIN.
    internal async Task<IGenericResult<List<(Dictionary<string, object?> ChildRow, Dictionary<string, object?>? ParentRow)>>> QueryConfigurationTableWithJoin(
        string schema,
        string childTableName,
        string parentTableName,
        string foreignKeyColumn,
        CancellationToken cancellationToken = default)
    {
        // Why: FK columns ending in "RowId" (e.g., DataContainerFieldRowId) reference the parent's
        // physical RowId. FK columns ending in "Id" (e.g., ConnectionId) reference the parent's
        // logical Id. The join column must match what the FK actually points to.
        var parentJoinColumn = foreignKeyColumn.EndsWith("RowId", StringComparison.Ordinal) ? "RowId" : "Id";
        var sql = string.Format(
            CultureInfo.InvariantCulture,
            "SELECT c.*, p.* FROM [{0}].[{1}] c LEFT JOIN [{0}].[{2}] p ON c.[{3}] = p.[{4}] AND p.[IsCurrent] = 1 AND p.[IsDeleted] = 0 WHERE c.[IsCurrent] = 1 AND c.[IsDeleted] = 0",
            schema,
            childTableName,
            parentTableName,
            foreignKeyColumn,
            parentJoinColumn);

        try
        {
            var rows = await QueryRowsWithJoinInternal(sql, cancellationToken).ConfigureAwait(false);
            return GenericResult<List<(Dictionary<string, object?> ChildRow, Dictionary<string, object?>? ParentRow)>>.Success(rows);
        }
        // Why: Table doesn't exist in this schema. Return empty success.
        // FDW014 suppressed: intentional — 208 is an expected condition, not an error to propagate.
#pragma warning disable FDW014
        catch (SqlException ex) when (ex.Number == 208)
        {
            return GenericResult<List<(Dictionary<string, object?> ChildRow, Dictionary<string, object?>? ParentRow)>>.Success([]);
        }
#pragma warning restore FDW014
        catch (SqlException ex)
        {
            MsSqlConnectionLogger.SqlExecutionError(Logger, sql, ex.Message, ex.Number);
            var handler = SqlErrorHandlers.ByErrorNumber(ex.Number);
            return GenericResult<List<(Dictionary<string, object?> ChildRow, Dictionary<string, object?>? ParentRow)>>.Failure(
                handler.CreateFailureMessage(_logger, ex, sql));
        }
    }

    /// <summary>
    /// Internal: executes a raw SQL query and returns rows as dictionaries.
    /// </summary>
    private async Task<List<Dictionary<string, object?>>> QueryRowsInternal(
        string sql,
        CancellationToken cancellationToken)
    {
        var results = new List<Dictionary<string, object?>>();

        var connection = CreatePooledConnection();
        await using (connection.ConfigureAwait(false))
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await SetUserSessionContext(connection, cancellationToken).ConfigureAwait(false);

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
                            row[reader.GetName(i)] = await reader.IsDBNullAsync(i, cancellationToken).ConfigureAwait(false) ? null : reader.GetValue(i);
                        }
                        results.Add(row);
                    }
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Internal: executes a raw SQL JOIN query and returns child/parent row pairs.
    /// </summary>
    private async Task<List<(Dictionary<string, object?> ChildRow, Dictionary<string, object?>? ParentRow)>> QueryRowsWithJoinInternal(
        string sql,
        CancellationToken cancellationToken)
    {
        var results = new List<(Dictionary<string, object?> ChildRow, Dictionary<string, object?>? ParentRow)>();

        var connection = CreatePooledConnection();
        await using (connection.ConfigureAwait(false))
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await SetUserSessionContext(connection, cancellationToken).ConfigureAwait(false);

            var command = new SqlCommand(sql, connection) { CommandTimeout = 30 };
            await using (command.ConfigureAwait(false))
            {
                var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                await using (reader.ConfigureAwait(false))
                {
                    // Discover child/parent column boundary.
                    // SQL Server returns c.* first, then p.*. First duplicate name = parent start.
                    var childColumnCount = 0;
                    var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        if (!seenNames.Add(reader.GetName(i)))
                        {
                            childColumnCount = i;
                            break;
                        }
                    }
                    if (childColumnCount == 0) childColumnCount = reader.FieldCount;

                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        var childRow = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                        for (var i = 0; i < childColumnCount; i++)
                        {
                            childRow[reader.GetName(i)] = await reader.IsDBNullAsync(i, cancellationToken).ConfigureAwait(false) ? null : reader.GetValue(i);
                        }

                        Dictionary<string, object?>? parentRow = null;
                        if (childColumnCount < reader.FieldCount && !await reader.IsDBNullAsync(childColumnCount, cancellationToken).ConfigureAwait(false))
                        {
                            parentRow = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                            for (var i = childColumnCount; i < reader.FieldCount; i++)
                            {
                                parentRow[reader.GetName(i)] = await reader.IsDBNullAsync(i, cancellationToken).ConfigureAwait(false) ? null : reader.GetValue(i);
                            }
                        }

                        results.Add((childRow, parentRow));
                    }
                }
            }
        }

        return results;
    }
}
