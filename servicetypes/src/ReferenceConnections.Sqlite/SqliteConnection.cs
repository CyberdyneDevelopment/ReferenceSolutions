using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
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

using Fdw.Services.Connections.Sqlite.Authentication;

using Fdw.Services.Connections.Sqlite.Commands;

using Fdw.Services.Connections.Sqlite.Validation;

namespace ReferenceConnections.Sqlite;

/// <summary>
/// SQLite data connection. Opens a new <see cref="SqliteAdoConnection"/> per operation
/// from the ADO.NET connection pool (via connection string sharing).
/// </summary>
[ExcludeFromCodeCoverage] // Excluded: requires live SQLite file or :memory: database
public sealed class SqliteConnection
    : ConnectionBase<SqliteCommand, SqliteConnectionConfiguration, SqliteConnection>,
      ITransactionalDataConnection,
      ISupportsHealthProbe
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
    public override bool IsStale => false;

    /// <inheritdoc/>
    protected override async Task<IGenericResult<T>> Execute<T>(
        SqliteCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken)
    {
        SqliteConnectionLog.TraceExecuteEntry(_logger, ((IGenericConfiguration)Configuration).Name);

        try
        {
            var adoConnection = CreateAdoConnection();
            await using (adoConnection.ConfigureAwait(false))
            {
                await adoConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
                command.Connection = adoConnection;

                SqliteConnectionLog.ExecutingCommand(
                    _logger,
                    ((IGenericConfiguration)Configuration).Name,
                    command.CommandText,
                    command.Parameters.Count);

                var isQuery = command.CommandText.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase);

                if (!isQuery)
                {
                    var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    SqliteConnectionLog.CommandCompleted(_logger, ((IGenericConfiguration)Configuration).Name, rowsAffected);
                    return GenericResult<T>.Success(ConvertScalarResult<T>(rowsAffected));
                }

                using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                var mappedResult = await MapReaderToType<T>(reader, container, cancellationToken).ConfigureAwait(false);
                SqliteConnectionLog.CommandCompleted(_logger, ((IGenericConfiguration)Configuration).Name, 0);
                return GenericResult<T>.Success(mappedResult);
            }
        }
        catch (SqliteException ex)
        {
            return GenericResult<T>.Failure(
                SqliteConnectionLog.ExecutionFailedWithException(_logger, ex, ((IGenericConfiguration)Configuration).Name));
        }
        catch (Exception ex)
        {
            return GenericResult<T>.Failure(
                SqliteConnectionLog.ExecutionFailedWithException(_logger, ex, ((IGenericConfiguration)Configuration).Name));
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
        var connectionName = ((IGenericConfiguration)Configuration).Name;
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
        var connectionName = ((IGenericConfiguration)Configuration).Name;
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
                SqliteConnectionLog.ConnectionFailedWithException(_logger, ex, connectionName));
        }
        catch (Exception ex)
        {
            return GenericResult.Failure(
                SqliteConnectionLog.ExecutionFailedWithException(_logger, ex, connectionName));
        }
    }

    internal SqliteAdoConnection CreateAdoConnection() => new SqliteAdoConnection(_connectionString);

    /// <summary>
    /// Internal static entry point used by <see cref="SqliteDataConnectionTransaction"/> to reuse
    /// the same reader-to-type mapping logic without needing a <see cref="SqliteConnection"/> instance.
    /// </summary>
    internal static async Task<T> MapReaderToTypeInternal<T>(
        Microsoft.Data.Sqlite.SqliteDataReader reader,
        IStorageContainer container,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var targetType = typeof(T);

        if (IsCollectionType(targetType, out var itemType))
        {
            var list = CreateElementList(itemType!, logger);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                list.Add(MapReaderRowToObject(reader, itemType!, container, logger));
            return (T)ConvertToCollectionType(list, targetType, itemType!);
        }

        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            return (T)MapReaderRowToObject(reader, targetType, container, logger);

        return default!;
    }

    private Task<T> MapReaderToType<T>(
        Microsoft.Data.Sqlite.SqliteDataReader reader,
        IStorageContainer container,
        CancellationToken cancellationToken)
        => MapReaderToTypeInternal<T>(reader, container, _logger, cancellationToken);

    private static object MapReaderRowToObject(
        Microsoft.Data.Sqlite.SqliteDataReader reader,
        Type targetType,
        IStorageContainer container,
        ILogger logger)
    {
        if (targetType.IsPrimitive || targetType == typeof(string) || targetType == typeof(DateTime)
            || targetType == typeof(decimal) || targetType == typeof(Guid))
        {
            return ConvertValue(reader.GetValue(0), targetType);
        }

        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            var dict = new Dictionary<string, object?>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
                dict[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            return dict;
        }

        var mapper = PocoMapperCollection.ByName(targetType.Name);
        if (mapper == PocoMapperCollection.NotFound)
        {
            var message = SqliteConnectionLog.NoMapperFound(logger, targetType.Name, targetType.FullName ?? targetType.Name);
            throw new InvalidOperationException(message.Message);
        }

        var mappingResult = mapper.MapFromReader(reader, container);
        if (!mappingResult.IsSuccess)
        {
            var message = SqliteConnectionLog.MappingFailed(logger, targetType.Name, mappingResult.CurrentMessage ?? "Unknown error");
            throw new InvalidOperationException(message.Message);
        }

        return mappingResult.Value!;
    }

    private static System.Collections.IList CreateElementList(Type itemType, ILogger logger)
    {
        var mapper = PocoMapperCollection.ByName(itemType.Name);
        if (mapper != PocoMapperCollection.NotFound)
            return mapper.CreateList();

        if (itemType.IsGenericType && itemType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            return new List<Dictionary<string, object?>>();

        var message = SqliteConnectionLog.NoMapperFound(logger, itemType.Name, itemType.FullName ?? itemType.Name);
        throw new InvalidOperationException(message.Message);
    }

    internal static T ConvertScalarResult<T>(int value)
    {
        var targetType = typeof(T);
        if (targetType == typeof(int)) return (T)(object)value;
        if (targetType == typeof(long)) return (T)(object)(long)value;
        if (targetType == typeof(bool)) return (T)(object)(value > 0);
        return default!;
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
}
