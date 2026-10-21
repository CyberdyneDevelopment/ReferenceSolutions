using System;
using Fdw.Data.RowSources.DataReader.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Data.Sqlite.Logging;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SqliteAdoConnection = Microsoft.Data.Sqlite.SqliteConnection;

using Fdw.Services.Connections.Sqlite;

namespace FdwReferenceConnections.Sqlite;

/// <summary>
/// SQLite data connection. Opens a new <see cref="SqliteAdoConnection"/> per operation
/// from the ADO.NET connection pool (via connection string sharing).
/// </summary>
[ExcludeFromCodeCoverage] // Excluded: requires live SQLite file or :memory: database
public sealed class SqliteConnection
    : ConnectionBase<SqliteCommand, SqliteConnectionConfiguration, SqliteConnection>,
      ITransactionalDataConnection,
      ISupportsHealthProbe,
      IRecordSourceConnection
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteConnection> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteConnection"/> class.
    /// </summary>
    public SqliteConnection(
        ILogger<SqliteConnection> logger,
        SqliteConnectionConfiguration configuration,
        string connectionString)
        : base(logger, configuration)
    {
        _connectionString = connectionString;
        _logger = logger ?? NullLogger<SqliteConnection>.Instance;
    }

    /// <inheritdoc/>
    protected override IDataCommandTranslator<SqliteCommand> GetTranslator(string commandType)
        => SqliteDataCommandTranslators.ByName(commandType);

    /// <summary>
    /// Gets the connection type identifier.
    /// </summary>
    public static string ConnectionType => "Sqlite";

    /// <inheritdoc/>
    protected override async Task<IGenericResult<T>> Execute<T>(
        SqliteCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken)
    {
        SqliteConnectionLog.TraceExecuteEntry(_logger, Configuration.Name);

        try
        {
            var adoConnection = CreateAdoConnection();
            await using (adoConnection.ConfigureAwait(false))
            {
                await adoConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
                command.Connection = adoConnection;

                SqliteConnectionLog.ExecutingCommand(
                    _logger,
                    Configuration.Name,
                    command.CommandText,
                    command.Parameters.Count);

                var isQuery = command.CommandText.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase);

                if (!isQuery)
                {
                    var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    SqliteConnectionLog.CommandCompleted(_logger, Configuration.Name, rowsAffected);
                    return GenericResult<T>.Success(ConvertScalarResult<T>(rowsAffected));
                }

                using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                var mappedResult = await MapReaderToType<T>(reader, container, cancellationToken).ConfigureAwait(false);
                if (!mappedResult.IsSuccess)
                {
                    return mappedResult;
                }

                SqliteConnectionLog.CommandCompleted(_logger, Configuration.Name, 0);
                return mappedResult;
            }
        }
        catch (SqliteException ex)
        {
            return GenericResult<T>.Failure(
                ExceptionResultExtensions.FlattenException(ex).Prepend(SqliteConnectionLog.ExecutionFailedWithException(_logger, ex, Configuration.Name)));
        }
        catch (Exception ex)
        {
            return GenericResult<T>.Failure(
                SqliteConnectionLog.ExecutionFailedWithException(_logger, ex, Configuration.Name));
        }
    }

    /// <inheritdoc/>
    protected override async Task<IGenericResult> Execute(
        SqliteCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken)
    {
        var result = await Execute<object>(command, container, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? GenericResult.Success() : result.ToNewResult<object>();
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IDataConnectionTransaction>> BeginTransaction(
        CancellationToken cancellationToken = default)
    {
        var connectionName = Configuration.Name;
        SqliteConnectionLog.TransactionBegan(_logger, connectionName);

        try
        {
            var adoConnection = CreateAdoConnection();
            await adoConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
            var transaction = await adoConnection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false)
                as Microsoft.Data.Sqlite.SqliteTransaction;

            return GenericResult<IDataConnectionTransaction>.Success(
                new SqliteDataConnectionTransaction(adoConnection, transaction!, _logger, connectionName));
        }
        catch (Exception ex)
        {
            return GenericResult<IDataConnectionTransaction>.Failure(
                SqliteConnectionLog.ConnectionFailedWithException(_logger, ex, connectionName));
        }
    }

    /// <summary>
    /// Performs a cheap liveness probe against SQLite by opening a connection and executing
    /// <c>SELECT 1</c>.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating whether the probe succeeded.</returns>
    public async Task<IGenericResult> Probe(CancellationToken cancellationToken = default)
    {
        var connectionName = Configuration.Name;
        SqliteConnectionLog.TraceProbeEntry(_logger, connectionName);

        try
        {
            var adoConnection = CreateAdoConnection();
            await using (adoConnection.ConfigureAwait(false))
            {
                await adoConnection.OpenAsync(cancellationToken).ConfigureAwait(false);

                var command = new SqliteCommand("SELECT 1", adoConnection);
                await using (command.ConfigureAwait(false))
                {
                    await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                }
            }

            SqliteConnectionLog.ProbeSucceeded(_logger, connectionName);
            return GenericResult.Success();
        }
        catch (SqliteException ex)
        {
            return GenericResult.Failure(
                ExceptionResultExtensions.FlattenException(ex).Prepend(SqliteConnectionLog.ConnectionFailedWithException(_logger, ex, connectionName)));
        }
        catch (Exception ex)
        {
            return GenericResult.Failure(
                SqliteConnectionLog.ExecutionFailedWithException(_logger, ex, connectionName));
        }
    }

    internal SqliteAdoConnection CreateAdoConnection() => new SqliteAdoConnection(_connectionString);

    // Why: the script extension (FdwReferenceConnections.Script, FDW-1447) logs where the connection logs, under its name.
    internal ILogger ScriptLogger => _logger;

    internal string ScriptConnectionName => Configuration.Name;

    /// <summary>
    /// Internal static entry point used by <see cref="SqliteDataConnectionTransaction"/> to reuse
    /// the same reader-to-type mapping logic without needing a <see cref="SqliteConnection"/> instance.
    /// </summary>
    internal static async Task<IGenericResult<T>> MapReaderToTypeInternal<T>(
        Microsoft.Data.Sqlite.SqliteDataReader reader,
        IStorageContainer container,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var targetType = typeof(T);

        if (IsCollectionType(targetType, out var itemType))
        {
            if (IsDataRowType(itemType!))
                return GenericResult<T>.Success((T)ConvertToCollectionType(
                    await ReadDataRows(reader, cancellationToken).ConfigureAwait(false), targetType, itemType!));

            var listResult = CreateElementList(itemType!, logger);
            if (!listResult.IsSuccess)
                return listResult.ToNewResult<T>();

            var list = listResult.Value!;
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var itemResult = MapReaderRowToObject(reader, itemType!, container, logger);
                if (!itemResult.IsSuccess)
                    return itemResult.ToNewResult<T>();
                list.Add(itemResult.Value);
            }

            return GenericResult<T>.Success((T)ConvertToCollectionType(list, targetType, itemType!));
        }

        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var itemResult = MapReaderRowToObject(reader, targetType, container, logger);
            return itemResult.IsSuccess ? GenericResult<T>.Success((T)itemResult.Value!) : itemResult.ToNewResult<T>();
        }

        return GenericResult<T>.Success(default!);
    }

    private Task<IGenericResult<T>> MapReaderToType<T>(
        Microsoft.Data.Sqlite.SqliteDataReader reader,
        IStorageContainer container,
        CancellationToken cancellationToken)
        => MapReaderToTypeInternal<T>(reader, container, _logger, cancellationToken);

    private static IGenericResult<object> MapReaderRowToObject(
        Microsoft.Data.Sqlite.SqliteDataReader reader,
        Type targetType,
        IStorageContainer container,
        ILogger logger)
    {
        if (targetType.IsPrimitive || targetType == typeof(string) || targetType == typeof(DateTime)
            || targetType == typeof(decimal) || targetType == typeof(Guid))
        {
            return GenericResult<object>.Success(ConvertValue(reader.GetValue(0), targetType));
        }

        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            var dict = new Dictionary<string, object?>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
                dict[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            return GenericResult<object>.Success(dict);
        }

        var mapper = PocoMapperCollection.ByName(targetType.Name);
        if (mapper == PocoMapperCollection.NotFound)
        {
            var message = SqliteConnectionLog.NoMapperFound(logger, targetType.Name, targetType.FullName ?? targetType.Name);
            return GenericResult<object>.Failure(message);
        }

        var mappingResult = mapper.MapFromReader(reader, container);
        if (!mappingResult.IsSuccess)
        {
            var message = SqliteConnectionLog.MappingFailed(logger, targetType.Name, mappingResult.CurrentMessage ?? "Unknown error");
            return GenericResult<object>.Failure(message);
        }

        return GenericResult<object>.Success(mappingResult.Value!);
    }

    private static IGenericResult<System.Collections.IList> CreateElementList(Type itemType, ILogger logger)
    {
        var mapper = PocoMapperCollection.ByName(itemType.Name);
        if (mapper != PocoMapperCollection.NotFound)
            return GenericResult<System.Collections.IList>.Success(mapper.CreateList());

        if (itemType.IsGenericType && itemType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            return GenericResult<System.Collections.IList>.Success(new List<Dictionary<string, object?>>());

        var message = SqliteConnectionLog.NoMapperFound(logger, itemType.Name, itemType.FullName ?? itemType.Name);
        return GenericResult<System.Collections.IList>.Failure(message);
    }

    internal static T ConvertScalarResult<T>(int value)
    {
        var targetType = typeof(T);
        if (targetType == typeof(int)) return (T)(object)value;
        if (targetType == typeof(long)) return (T)(object)(long)value;
        if (targetType == typeof(bool)) return (T)(object)(value > 0);
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
        Microsoft.Data.Sqlite.SqliteDataReader reader, CancellationToken cancellationToken)
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
            if (genericTypeDef == typeof(IEnumerable<>) || genericTypeDef == typeof(List<>)
                || genericTypeDef == typeof(IList<>) || genericTypeDef == typeof(ICollection<>))
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
        return list;
    }

    private static object ConvertValue(object value, Type targetType)
    {
        var underlyingType = Nullable.GetUnderlyingType(targetType);

        if (value == null || value == DBNull.Value)
        {
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
    /// <inheritdoc />
    /// <remarks>The streaming counterpart of Execute: the cursor keeps the connection open while read.</remarks>
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

        var sqliteCommand = translationResult.Value;
        Microsoft.Data.Sqlite.SqliteConnection? adoConnection = null;
        Microsoft.Data.Sqlite.SqliteDataReader? reader = null;
        try
        {
            adoConnection = CreateAdoConnection();
            await adoConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
            sqliteCommand.Connection = adoConnection;

            SqliteConnectionLog.ExecutingCommand(
                _logger,
                Configuration.Name,
                sqliteCommand.CommandText,
                sqliteCommand.Parameters.Count);

            reader = await sqliteCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            var fields = container.Schema.Fields.OfType<Fdw.Data.Abstractions.IDataField>().ToList();
            var cursor = new CursorRecordSource(new DataReaderRowSource(reader), fields, _logger);
            var scoped = new SqliteRecordSource(cursor, sqliteCommand, reader, adoConnection);
            reader = null;
            adoConnection = null;
            return GenericResult<IRecordSource<DataRecord>>.Success(scoped);
        }
        catch (Microsoft.Data.Sqlite.SqliteException ex)
        {
            if (reader is not null) await reader.DisposeAsync().ConfigureAwait(false);
            if (adoConnection is not null) await adoConnection.DisposeAsync().ConfigureAwait(false);
            return GenericResult<IRecordSource<DataRecord>>.Failure(
                ExceptionResultExtensions.FlattenException(ex).Prepend(SqliteConnectionLog.ExecutionFailedWithException(_logger, ex, Configuration.Name)));
        }
    }
}
