using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections.PostgreSql.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

using Fdw.Services.Connections.PostgreSql;
using Fdw.Services.Connections.PostgreSql.Discovery;

using ReferenceConnections.PostgreSql;

namespace ReferenceConnections.PostgreSql.Discovery;

/// <summary>
/// Implementation of schema discovery for PostgreSQL databases.
/// Queries information_schema and pg_catalog views to discover tables, views, columns, and indexes.
/// Uses batched queries to minimize round trips.
/// </summary>
[ExcludeFromCodeCoverage] // Excluded: requires PostgreSQL connection
public sealed class PostgreSqlSchemaDiscoverer : IPostgreSqlSchemaDiscoverer
{
    private readonly ILogger<PostgreSqlSchemaDiscoverer> _logger;

    // Compiled regex for LIKE pattern matching with timeout for DoS protection
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    #region SQL Queries

    private const string DatabaseNameQuery = "SELECT current_database()";

    private const string SchemasQuery = @"
        SELECT schema_name
        FROM information_schema.schemata
        WHERE schema_name NOT IN ('pg_catalog', 'information_schema', 'pg_toast')
        ORDER BY schema_name";

    private const string AllTablesQuery = @"
        SELECT table_schema, table_name, table_type
        FROM information_schema.tables
        WHERE table_schema NOT IN ('pg_catalog', 'information_schema', 'pg_toast')
        ORDER BY table_schema, table_name";

    private const string AllColumnsQuery = @"
        SELECT
            c.table_schema, c.table_name,
            c.column_name, c.data_type, c.is_nullable,
            c.character_maximum_length, c.numeric_precision, c.numeric_scale,
            c.column_default, c.ordinal_position,
            CASE WHEN c.is_identity = 'YES' THEN true ELSE false END AS is_identity,
            CASE WHEN c.is_generated = 'ALWAYS' THEN true ELSE false END AS is_generated
        FROM information_schema.columns c
        WHERE c.table_schema NOT IN ('pg_catalog', 'information_schema', 'pg_toast')
        ORDER BY c.table_schema, c.table_name, c.ordinal_position";

    private const string AllPrimaryKeysQuery = @"
        SELECT tc.table_schema, tc.table_name, kcu.column_name, kcu.ordinal_position
        FROM information_schema.table_constraints tc
        JOIN information_schema.key_column_usage kcu
            ON tc.constraint_name = kcu.constraint_name
            AND tc.table_schema = kcu.table_schema
            AND tc.table_name = kcu.table_name
        WHERE tc.constraint_type = 'PRIMARY KEY'
            AND tc.table_schema NOT IN ('pg_catalog', 'information_schema', 'pg_toast')
        ORDER BY tc.table_schema, tc.table_name, kcu.ordinal_position";

    private const string AllIndexesQuery = @"
        SELECT
            n.nspname AS schema_name,
            t.relname AS table_name,
            i.relname AS index_name,
            a.attname AS column_name,
            ix.indisunique AS is_unique,
            ix.indisprimary AS is_primary,
            array_position(ix.indkey, a.attnum) AS key_ordinal
        FROM pg_catalog.pg_index ix
        JOIN pg_catalog.pg_class i ON ix.indexrelid = i.oid
        JOIN pg_catalog.pg_class t ON ix.indrelid = t.oid
        JOIN pg_catalog.pg_namespace n ON t.relnamespace = n.oid
        JOIN pg_catalog.pg_attribute a ON a.attrelid = t.oid AND a.attnum = ANY(ix.indkey)
        WHERE n.nspname NOT IN ('pg_catalog', 'information_schema', 'pg_toast')
            AND i.relname IS NOT NULL
        ORDER BY n.nspname, t.relname, i.relname, array_position(ix.indkey, a.attnum)";

    private const string AllForeignKeysQuery = @"
        SELECT
            nsp.nspname AS schema_name,
            rel.relname AS table_name,
            con.conname AS foreign_key_name,
            att.attname AS column_name,
            rnsp.nspname AS referenced_schema,
            rrel.relname AS referenced_table,
            ratt.attname AS referenced_column,
            con.confdeltype AS delete_action,
            con.confupdtype AS update_action,
            attord.ord AS constraint_column_id
        FROM pg_catalog.pg_constraint con
        JOIN pg_catalog.pg_class rel ON con.conrelid = rel.oid
        JOIN pg_catalog.pg_namespace nsp ON rel.relnamespace = nsp.oid
        JOIN pg_catalog.pg_class rrel ON con.confrelid = rrel.oid
        JOIN pg_catalog.pg_namespace rnsp ON rrel.relnamespace = rnsp.oid
        CROSS JOIN LATERAL unnest(con.conkey, con.confkey) WITH ORDINALITY AS attord(conkey, confkey, ord)
        JOIN pg_catalog.pg_attribute att ON att.attrelid = rel.oid AND att.attnum = attord.conkey
        JOIN pg_catalog.pg_attribute ratt ON ratt.attrelid = rrel.oid AND ratt.attnum = attord.confkey
        WHERE con.contype = 'f'
            AND nsp.nspname NOT IN ('pg_catalog', 'information_schema', 'pg_toast')
        ORDER BY nsp.nspname, rel.relname, con.conname, attord.ord";

    private const string AllDescriptionsQuery = @"
        SELECT
            n.nspname AS schema_name,
            c.relname AS table_name,
            d.description
        FROM pg_catalog.pg_description d
        JOIN pg_catalog.pg_class c ON d.objoid = c.oid
        JOIN pg_catalog.pg_namespace n ON c.relnamespace = n.oid
        WHERE d.objsubid = 0
            AND n.nspname NOT IN ('pg_catalog', 'information_schema', 'pg_toast')";

    // Per-table queries used by DiscoverContainer (single container discovery)
    private const string SingleTableColumnsQuery = @"
        SELECT
            c.column_name, c.data_type, c.is_nullable,
            c.character_maximum_length, c.numeric_precision, c.numeric_scale,
            c.column_default, c.ordinal_position,
            CASE WHEN c.is_identity = 'YES' THEN true ELSE false END AS is_identity,
            CASE WHEN c.is_generated = 'ALWAYS' THEN true ELSE false END AS is_generated
        FROM information_schema.columns c
        WHERE c.table_schema = @SchemaName AND c.table_name = @ObjectName
        ORDER BY c.ordinal_position";

    private const string SingleTablePrimaryKeyQuery = @"
        SELECT kcu.column_name
        FROM information_schema.table_constraints tc
        JOIN information_schema.key_column_usage kcu
            ON tc.constraint_name = kcu.constraint_name
            AND tc.table_schema = kcu.table_schema
            AND tc.table_name = kcu.table_name
        WHERE tc.constraint_type = 'PRIMARY KEY'
            AND tc.table_schema = @SchemaName
            AND tc.table_name = @ObjectName
        ORDER BY kcu.ordinal_position";

    private const string SingleTableIndexesQuery = @"
        SELECT
            i.relname AS index_name,
            a.attname AS column_name,
            ix.indisunique AS is_unique,
            ix.indisprimary AS is_primary
        FROM pg_catalog.pg_index ix
        JOIN pg_catalog.pg_class i ON ix.indexrelid = i.oid
        JOIN pg_catalog.pg_class t ON ix.indrelid = t.oid
        JOIN pg_catalog.pg_namespace n ON t.relnamespace = n.oid
        JOIN pg_catalog.pg_attribute a ON a.attrelid = t.oid AND a.attnum = ANY(ix.indkey)
        WHERE n.nspname = @SchemaName AND t.relname = @ObjectName AND i.relname IS NOT NULL
        ORDER BY i.relname, array_position(ix.indkey, a.attnum)";

    private const string SingleTableObjectTypeQuery = @"
        SELECT table_type
        FROM information_schema.tables
        WHERE table_schema = @SchemaName AND table_name = @ObjectName";

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlSchemaDiscoverer"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public PostgreSqlSchemaDiscoverer(ILogger<PostgreSqlSchemaDiscoverer>? logger = null)
    {
        _logger = logger ?? NullLogger<PostgreSqlSchemaDiscoverer>.Instance;
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Method is too long - sequential schema discovery with batched queries and in-memory assembly
    public async Task<IGenericResult<SchemaDiscoveryResult>> DiscoverSchema(
        PostgreSqlConnection connection,
        SchemaDiscoveryOptions? options,
        CancellationToken cancellationToken)
#pragma warning restore MA0051
    {
        options ??= SchemaDiscoveryOptions.Default;

        try
        {
            // Ensure connection is open
            var connectResult = await connection.Connect(cancellationToken).ConfigureAwait(false);
            if (!connectResult.IsSuccess)
            {
                return GenericResult<SchemaDiscoveryResult>.Failure(
                    PostgreSqlSchemaDiscoveryLog.ConnectionFailed(_logger, connectResult.CurrentMessage ?? "Unknown error"));
            }

            var npgsqlConnection = connection.InnerConnection;

            // 1. Get database name
            var databaseName = await GetDatabaseName(npgsqlConnection, cancellationToken).ConfigureAwait(false);
            PostgreSqlSchemaDiscoveryLog.SchemaDiscoveryStarted(_logger, databaseName);

            // 2. Discover and filter schemas
            var schemas = await DiscoverSchemasInternal(npgsqlConnection, options, cancellationToken).ConfigureAwait(false);
            if (schemas.Count == 0)
            {
                return GenericResult<SchemaDiscoveryResult>.Success(new SchemaDiscoveryResult
                {
                    DatabaseName = databaseName,
                    Paths = [],
                    TotalContainers = 0,
                    TotalFields = 0
                });
            }

            PostgreSqlSchemaDiscoveryLog.SchemasDiscovered(_logger, schemas.Count);

            // 3. Batch query: ALL tables/views across ALL schemas
            var allTables = await DiscoverAllTablesInternal(npgsqlConnection, options, schemas, cancellationToken).ConfigureAwait(false);
            if (allTables.Count == 0)
            {
                return GenericResult<SchemaDiscoveryResult>.Success(new SchemaDiscoveryResult
                {
                    DatabaseName = databaseName,
                    Paths = [],
                    TotalContainers = 0,
                    TotalFields = 0
                });
            }

            // Build a set of (schema, table) keys for efficient lookup
            var tableKeys = new HashSet<(string Schema, string Table)>(
                allTables.Select(t => (t.Schema, t.Table)),
                TableKeyComparer.Instance);

            // 4. Batch query: ALL columns across ALL tables
            var allColumns = await DiscoverAllColumnsInternal(npgsqlConnection, tableKeys, cancellationToken).ConfigureAwait(false);

            // 5. Batch query: ALL primary keys across ALL tables
            var allPrimaryKeys = await DiscoverAllPrimaryKeysInternal(npgsqlConnection, tableKeys, cancellationToken).ConfigureAwait(false);

            // 6. Batch query: ALL indexes (optional)
            var allIndexes = options.DiscoverIndexes
                ? await DiscoverAllIndexesInternal(npgsqlConnection, tableKeys, cancellationToken).ConfigureAwait(false)
                : new Dictionary<(string Schema, string Table), List<DiscoveredIndex>>(TableKeyComparer.Instance);

            // 7. Batch query: ALL foreign keys (optional)
            var allForeignKeys = options.DiscoverForeignKeys
                ? await DiscoverAllForeignKeysInternal(npgsqlConnection, tableKeys, cancellationToken).ConfigureAwait(false)
                : new Dictionary<(string Schema, string Table), List<DiscoveredForeignKey>>(TableKeyComparer.Instance);

            // 8. Batch query: ALL descriptions (optional)
            var allDescriptions = options.DiscoverDescriptions
                ? await DiscoverAllDescriptionsInternal(npgsqlConnection, tableKeys, cancellationToken).ConfigureAwait(false)
                : new Dictionary<(string Schema, string Table), string>(TableKeyComparer.Instance);

            // Assemble results
            var result = AssembleDiscoveryResult(
                databaseName, allTables, allColumns, allPrimaryKeys, allIndexes, allForeignKeys, allDescriptions);

            PostgreSqlSchemaDiscoveryLog.SchemaDiscoveryCompleted(_logger, result.Paths.Count, result.TotalContainers, result.TotalFields);

            return GenericResult<SchemaDiscoveryResult>.Success(result);
        }
        catch (Exception ex)
        {
            return GenericResult<SchemaDiscoveryResult>.Failure(
                PostgreSqlSchemaDiscoveryLog.SchemaDiscoveryFailed(_logger, ex.Message));
        }
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Method is too long - sequential container discovery and field mapping
    public async Task<IGenericResult<DiscoveredContainer>> DiscoverContainer(
        PostgreSqlConnection connection,
        string schemaName,
        string objectName,
        CancellationToken cancellationToken)
#pragma warning restore MA0051
    {
        try
        {
            var connectResult = await connection.Connect(cancellationToken).ConfigureAwait(false);
            if (!connectResult.IsSuccess)
            {
                return GenericResult<DiscoveredContainer>.Failure(
                    PostgreSqlSchemaDiscoveryLog.ConnectionFailed(_logger, connectResult.CurrentMessage ?? "Unknown error"));
            }

            PostgreSqlSchemaDiscoveryLog.ContainerDiscoveryStarted(_logger, schemaName, objectName);

            var npgsqlConnection = connection.InnerConnection;

            // Get object type
            var objectType = await GetObjectType(npgsqlConnection, schemaName, objectName, cancellationToken).ConfigureAwait(false);
            if (objectType == null)
            {
                return GenericResult<DiscoveredContainer>.Failure(
                    PostgreSqlSchemaDiscoveryLog.ContainerNotFound(_logger, schemaName, objectName));
            }

            // Discover columns
            var columns = await DiscoverSingleTableColumns(npgsqlConnection, schemaName, objectName, cancellationToken).ConfigureAwait(false);

            // Discover primary key
            var primaryKeyColumns = await DiscoverSingleTablePrimaryKey(npgsqlConnection, schemaName, objectName, cancellationToken).ConfigureAwait(false);

            // Mark primary key columns
            var fields = MarkPrimaryKeyFields(columns, primaryKeyColumns);

            // Discover indexes
            var indexes = await DiscoverSingleTableIndexes(npgsqlConnection, schemaName, objectName, cancellationToken).ConfigureAwait(false);

            var container = new DiscoveredContainer
            {
                Name = objectName,
                ContainerType = objectType,
                Fields = fields,
                PrimaryKeyColumns = primaryKeyColumns,
                Indexes = indexes
            };
            container.SchemaName = schemaName;

            PostgreSqlSchemaDiscoveryLog.ContainerDiscovered(_logger, objectType, schemaName, objectName, fields.Count);

            return GenericResult<DiscoveredContainer>.Success(container);
        }
        catch (Exception ex)
        {
            return GenericResult<DiscoveredContainer>.Failure(
                PostgreSqlSchemaDiscoveryLog.ContainerDiscoveryFailed(_logger, schemaName, objectName, ex.Message));
        }
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<bool>> TestDiscoveryCapability(
        PostgreSqlConnection connection,
        CancellationToken cancellationToken)
    {
        try
        {
            var connectResult = await connection.Connect(cancellationToken).ConfigureAwait(false);
            if (!connectResult.IsSuccess)
            {
                return GenericResult<bool>.Failure(
                    PostgreSqlSchemaDiscoveryLog.ConnectionFailed(_logger, connectResult.CurrentMessage ?? "Unknown error"));
            }

            // Test by querying information_schema
            const string testQuery = "SELECT schema_name FROM information_schema.schemata LIMIT 1";
            var npgsqlConnection = connection.InnerConnection;

            using var command = new NpgsqlCommand(testQuery, npgsqlConnection);
            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

            return GenericResult<bool>.Success(result != null);
        }
        catch (Exception ex)
        {
            return GenericResult<bool>.Failure(
                PostgreSqlSchemaDiscoveryLog.DiscoveryTestFailed(_logger, ex.Message));
        }
    }

    #region Batched Discovery Internals

    private static async Task<string> GetDatabaseName(NpgsqlConnection npgsqlConnection, CancellationToken cancellationToken)
    {
        using var command = new NpgsqlCommand(DatabaseNameQuery, npgsqlConnection);
        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return result?.ToString() ?? "Unknown";
    }

    private static async Task<List<string>> DiscoverSchemasInternal(
        NpgsqlConnection npgsqlConnection,
        SchemaDiscoveryOptions options,
        CancellationToken cancellationToken)
    {
        var schemas = new List<string>();
        using var command = new NpgsqlCommand(SchemasQuery, npgsqlConnection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var schemaName = reader.GetString(0);

            // Apply exclusion filter
            if (options.ExcludedSchemas.Any(s => string.Equals(s, schemaName, StringComparison.OrdinalIgnoreCase)))
                continue;

            // Apply inclusion filter if specified
            if (options.IncludeOnlySchemas != null && options.IncludeOnlySchemas.Count > 0)
            {
                if (!options.IncludeOnlySchemas.Any(s => string.Equals(s, schemaName, StringComparison.OrdinalIgnoreCase)))
                    continue;
            }

            schemas.Add(schemaName);
        }

        return schemas;
    }

    private static async Task<List<(string Schema, string Table, string ContainerType)>> DiscoverAllTablesInternal(
        NpgsqlConnection npgsqlConnection,
        SchemaDiscoveryOptions options,
        List<string> filteredSchemas,
        CancellationToken cancellationToken)
    {
        var schemaSet = new HashSet<string>(filteredSchemas, StringComparer.OrdinalIgnoreCase);
        var perSchemaCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var tables = new List<(string Schema, string Table, string ContainerType)>();

        using var command = new NpgsqlCommand(AllTablesQuery, npgsqlConnection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var schema = reader.GetString(0);
            var tableName = reader.GetString(1);
            var tableType = reader.GetString(2);

            // Only include schemas that passed filtering
            if (!schemaSet.Contains(schema))
                continue;

            // Filter views if not requested
            if (!options.DiscoverViews && string.Equals(tableType, "VIEW", StringComparison.OrdinalIgnoreCase))
                continue;

            // Apply exclusion patterns
            if (options.ExcludedTablePatterns != null)
            {
                var excluded = false;
                foreach (var pattern in options.ExcludedTablePatterns)
                {
                    if (MatchesLikePattern(tableName, pattern))
                    {
                        excluded = true;
                        break;
                    }
                }
                if (excluded) continue;
            }

            // Apply max tables per schema limit
            if (options.MaxTablesPerSchema > 0)
            {
                perSchemaCount.TryGetValue(schema, out var count);
                if (count >= options.MaxTablesPerSchema)
                    continue;
                perSchemaCount[schema] = count + 1;
            }

            var containerType = string.Equals(tableType, "VIEW", StringComparison.OrdinalIgnoreCase)
                ? "View"
                : "Table";

            tables.Add((schema, tableName, containerType));
        }

        return tables;
    }

    private static async Task<Dictionary<(string Schema, string Table), List<DiscoveredField>>> DiscoverAllColumnsInternal(
        NpgsqlConnection npgsqlConnection,
        HashSet<(string Schema, string Table)> tableKeys,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<(string Schema, string Table), List<DiscoveredField>>(TableKeyComparer.Instance);

        using var command = new NpgsqlCommand(AllColumnsQuery, npgsqlConnection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var schema = reader.GetString(0);
            var table = reader.GetString(1);
            var key = (schema, table);

            // Only include columns for tables that passed filtering
            if (!tableKeys.Contains(key))
                continue;

            var field = new DiscoveredField
            {
                Name = reader.GetString(2),
                SqlType = reader.GetString(3),
                IsNullable = string.Equals(reader.GetString(4), "YES", StringComparison.OrdinalIgnoreCase),
                MaxLength = await reader.IsDBNullAsync(5, cancellationToken).ConfigureAwait(false) ? null : reader.GetInt32(5),
                Precision = await reader.IsDBNullAsync(6, cancellationToken).ConfigureAwait(false) ? null : reader.GetInt32(6),
                Scale = await reader.IsDBNullAsync(7, cancellationToken).ConfigureAwait(false) ? null : reader.GetInt32(7),
                DefaultValue = await reader.IsDBNullAsync(8, cancellationToken).ConfigureAwait(false) ? null : reader.GetString(8),
                Ordinal = reader.GetInt32(9),
                IsIdentity = reader.GetBoolean(10),
                IsComputed = reader.GetBoolean(11)
            };

            if (!result.TryGetValue(key, out var list))
            {
                list = [];
                result[key] = list;
            }
            list.Add(field);
        }

        return result;
    }

    private static async Task<Dictionary<(string Schema, string Table), List<string>>> DiscoverAllPrimaryKeysInternal(
        NpgsqlConnection npgsqlConnection,
        HashSet<(string Schema, string Table)> tableKeys,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<(string Schema, string Table), List<string>>(TableKeyComparer.Instance);

        using var command = new NpgsqlCommand(AllPrimaryKeysQuery, npgsqlConnection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var schema = reader.GetString(0);
            var table = reader.GetString(1);
            var key = (schema, table);

            if (!tableKeys.Contains(key))
                continue;

            var columnName = reader.GetString(2);

            if (!result.TryGetValue(key, out var list))
            {
                list = [];
                result[key] = list;
            }
            list.Add(columnName);
        }

        return result;
    }

    private static async Task<Dictionary<(string Schema, string Table), List<DiscoveredIndex>>> DiscoverAllIndexesInternal(
        NpgsqlConnection npgsqlConnection,
        HashSet<(string Schema, string Table)> tableKeys,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<(string Schema, string Table), List<DiscoveredIndex>>(TableKeyComparer.Instance);
        var indexColumns = new Dictionary<(string Schema, string Table, string IndexName), (bool IsUnique, bool IsPrimary, List<string> Columns)>();

        using var command = new NpgsqlCommand(AllIndexesQuery, npgsqlConnection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var schema = reader.GetString(0);
            var table = reader.GetString(1);
            var tableKey = (schema, table);

            if (!tableKeys.Contains(tableKey))
                continue;

            var indexName = reader.GetString(2);
            var columnName = reader.GetString(3);
            var isUnique = reader.GetBoolean(4);
            var isPrimary = reader.GetBoolean(5);

            var indexKey = (schema, table, indexName);
            if (!indexColumns.TryGetValue(indexKey, out var indexData))
            {
                indexData = (isUnique, isPrimary, []);
                indexColumns[indexKey] = indexData;
            }
            indexData.Columns.Add(columnName);
        }

        // Group by table
        foreach (var ((schema, table, indexName), (isUnique, isPrimary, columns)) in indexColumns)
        {
            var tableKey = (schema, table);
            if (!result.TryGetValue(tableKey, out var list))
            {
                list = [];
                result[tableKey] = list;
            }

            list.Add(new DiscoveredIndex
            {
                Name = indexName,
                Columns = columns,
                IsUnique = isUnique,
                IsPrimaryKey = isPrimary
            });
        }

        return result;
    }

    private static async Task<Dictionary<(string Schema, string Table), List<DiscoveredForeignKey>>> DiscoverAllForeignKeysInternal(
        NpgsqlConnection npgsqlConnection,
        HashSet<(string Schema, string Table)> tableKeys,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<(string Schema, string Table), List<DiscoveredForeignKey>>(TableKeyComparer.Instance);
        var fkData = new Dictionary<(string Schema, string Table, string FkName), (string RefSchema, string RefTable, char DeleteAction, char UpdateAction, List<DiscoveredForeignKeyColumn> Columns)>();

        using var command = new NpgsqlCommand(AllForeignKeysQuery, npgsqlConnection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var schema = reader.GetString(0);
            var table = reader.GetString(1);
            var tableKey = (schema, table);

            if (!tableKeys.Contains(tableKey))
                continue;

            var fkName = reader.GetString(2);
            var columnName = reader.GetString(3);
            var refSchema = reader.GetString(4);
            var refTable = reader.GetString(5);
            var refColumn = reader.GetString(6);
            var deleteAction = reader.GetChar(7);
            var updateAction = reader.GetChar(8);
            var ordinal = reader.GetInt32(9);

            var fkKey = (schema, table, fkName);
            if (!fkData.TryGetValue(fkKey, out var data))
            {
                data = (refSchema, refTable, deleteAction, updateAction, []);
                fkData[fkKey] = data;
            }

            data.Columns.Add(new DiscoveredForeignKeyColumn
            {
                ColumnName = columnName,
                ReferencedColumnName = refColumn,
                Ordinal = ordinal
            });
        }

        // Group by table
        foreach (var ((schema, table, fkName), (refSchema, refTable, deleteAction, updateAction, columns)) in fkData)
        {
            var tableKey = (schema, table);
            if (!result.TryGetValue(tableKey, out var list))
            {
                list = [];
                result[tableKey] = list;
            }

            list.Add(new DiscoveredForeignKey
            {
                Name = fkName,
                Columns = columns,
                ReferencedSchema = refSchema,
                ReferencedTable = refTable,
                OnDelete = MapForeignKeyAction(deleteAction),
                OnUpdate = MapForeignKeyAction(updateAction)
            });
        }

        return result;
    }

    private static async Task<Dictionary<(string Schema, string Table), string>> DiscoverAllDescriptionsInternal(
        NpgsqlConnection npgsqlConnection,
        HashSet<(string Schema, string Table)> tableKeys,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<(string Schema, string Table), string>(TableKeyComparer.Instance);

        using var command = new NpgsqlCommand(AllDescriptionsQuery, npgsqlConnection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var schema = reader.GetString(0);
            var table = reader.GetString(1);
            var key = (schema, table);

            if (!tableKeys.Contains(key))
                continue;

            if (!await reader.IsDBNullAsync(2, cancellationToken).ConfigureAwait(false))
            {
                result[key] = reader.GetString(2);
            }
        }

        return result;
    }

    #endregion

    #region Single Container Discovery

    private static async Task<string?> GetObjectType(
        NpgsqlConnection npgsqlConnection,
        string schemaName,
        string objectName,
        CancellationToken cancellationToken)
    {
        using var command = new NpgsqlCommand(SingleTableObjectTypeQuery, npgsqlConnection);
        command.Parameters.AddWithValue("SchemaName", schemaName);
        command.Parameters.AddWithValue("ObjectName", objectName);

        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        if (result == null || result == DBNull.Value)
            return null;

        var tableType = result.ToString()!;
        return string.Equals(tableType, "VIEW", StringComparison.OrdinalIgnoreCase) ? "View" : "Table";
    }

    private static async Task<List<DiscoveredField>> DiscoverSingleTableColumns(
        NpgsqlConnection npgsqlConnection,
        string schemaName,
        string objectName,
        CancellationToken cancellationToken)
    {
        var fields = new List<DiscoveredField>();

        using var command = new NpgsqlCommand(SingleTableColumnsQuery, npgsqlConnection);
        command.Parameters.AddWithValue("SchemaName", schemaName);
        command.Parameters.AddWithValue("ObjectName", objectName);

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            fields.Add(new DiscoveredField
            {
                Name = reader.GetString(0),
                SqlType = reader.GetString(1),
                IsNullable = string.Equals(reader.GetString(2), "YES", StringComparison.OrdinalIgnoreCase),
                MaxLength = await reader.IsDBNullAsync(3, cancellationToken).ConfigureAwait(false) ? null : reader.GetInt32(3),
                Precision = await reader.IsDBNullAsync(4, cancellationToken).ConfigureAwait(false) ? null : reader.GetInt32(4),
                Scale = await reader.IsDBNullAsync(5, cancellationToken).ConfigureAwait(false) ? null : reader.GetInt32(5),
                DefaultValue = await reader.IsDBNullAsync(6, cancellationToken).ConfigureAwait(false) ? null : reader.GetString(6),
                Ordinal = reader.GetInt32(7),
                IsIdentity = reader.GetBoolean(8),
                IsComputed = reader.GetBoolean(9)
            });
        }

        return fields;
    }

    private static async Task<List<string>> DiscoverSingleTablePrimaryKey(
        NpgsqlConnection npgsqlConnection,
        string schemaName,
        string objectName,
        CancellationToken cancellationToken)
    {
        var columns = new List<string>();

        using var command = new NpgsqlCommand(SingleTablePrimaryKeyQuery, npgsqlConnection);
        command.Parameters.AddWithValue("SchemaName", schemaName);
        command.Parameters.AddWithValue("ObjectName", objectName);

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            columns.Add(reader.GetString(0));
        }

        return columns;
    }

    private static async Task<List<DiscoveredIndex>> DiscoverSingleTableIndexes(
        NpgsqlConnection npgsqlConnection,
        string schemaName,
        string objectName,
        CancellationToken cancellationToken)
    {
        var indexColumns = new Dictionary<string, (bool IsUnique, bool IsPrimary, List<string> Columns)>(StringComparer.Ordinal);

        using var command = new NpgsqlCommand(SingleTableIndexesQuery, npgsqlConnection);
        command.Parameters.AddWithValue("SchemaName", schemaName);
        command.Parameters.AddWithValue("ObjectName", objectName);

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var indexName = reader.GetString(0);
            var columnName = reader.GetString(1);
            var isUnique = reader.GetBoolean(2);
            var isPrimary = reader.GetBoolean(3);

            if (!indexColumns.TryGetValue(indexName, out var data))
            {
                data = (isUnique, isPrimary, []);
                indexColumns[indexName] = data;
            }
            data.Columns.Add(columnName);
        }

        var indexes = new List<DiscoveredIndex>();
        foreach (var (indexName, (isUnique, isPrimary, columns)) in indexColumns)
        {
            indexes.Add(new DiscoveredIndex
            {
                Name = indexName,
                Columns = columns,
                IsUnique = isUnique,
                IsPrimaryKey = isPrimary
            });
        }

        return indexes;
    }

    #endregion

    #region Assembly & Helpers

    private SchemaDiscoveryResult AssembleDiscoveryResult(
        string databaseName,
        List<(string Schema, string Table, string ContainerType)> allTables,
        Dictionary<(string Schema, string Table), List<DiscoveredField>> allColumns,
        Dictionary<(string Schema, string Table), List<string>> allPrimaryKeys,
        Dictionary<(string Schema, string Table), List<DiscoveredIndex>> allIndexes,
        Dictionary<(string Schema, string Table), List<DiscoveredForeignKey>> allForeignKeys,
        Dictionary<(string Schema, string Table), string> allDescriptions)
    {
        var pathMap = new Dictionary<string, List<DiscoveredContainer>>(StringComparer.OrdinalIgnoreCase);
        var totalFields = 0;

        foreach (var (schema, table, containerType) in allTables)
        {
            var tableKey = (schema, table);

            // Get columns and mark primary keys
            var columns = allColumns.TryGetValue(tableKey, out var cols) ? cols : [];
            var pkColumns = allPrimaryKeys.TryGetValue(tableKey, out var pks) ? pks : [];
            var fields = MarkPrimaryKeyFields(columns, pkColumns);
            totalFields += fields.Count;

            // Get indexes
            var indexes = allIndexes.TryGetValue(tableKey, out var idx) ? idx : [];

            // Get foreign keys
            var foreignKeys = allForeignKeys.TryGetValue(tableKey, out var fks) ? fks : [];

            // Get description
            allDescriptions.TryGetValue(tableKey, out var description);

            var container = new DiscoveredContainer
            {
                Name = table,
                ContainerType = containerType,
                Fields = fields,
                PrimaryKeyColumns = pkColumns,
                Indexes = indexes,
                ForeignKeys = foreignKeys,
                Description = description
            };
            container.SchemaName = schema;

            PostgreSqlSchemaDiscoveryLog.ContainerDiscovered(_logger, containerType, schema, table, fields.Count);

            if (!pathMap.TryGetValue(schema, out var containerList))
            {
                containerList = [];
                pathMap[schema] = containerList;
            }
            containerList.Add(container);
        }

        var paths = new List<DiscoveredPath>();
        foreach (var (schemaName, containers) in pathMap)
        {
            paths.Add(new DiscoveredPath
            {
                SchemaName = schemaName,
                Containers = containers
            });
        }

        return new SchemaDiscoveryResult
        {
            DatabaseName = databaseName,
            Paths = paths,
            TotalContainers = allTables.Count,
            TotalFields = totalFields
        };
    }

    private static List<DiscoveredField> MarkPrimaryKeyFields(
        List<DiscoveredField> columns,
        List<string> primaryKeyColumns)
    {
        if (primaryKeyColumns.Count == 0)
            return columns;

        var pkSet = new HashSet<string>(primaryKeyColumns, StringComparer.OrdinalIgnoreCase);
        var result = new List<DiscoveredField>(columns.Count);

        foreach (var col in columns)
        {
            if (pkSet.Contains(col.Name))
            {
                result.Add(new DiscoveredField
                {
                    Name = col.Name,
                    SqlType = col.SqlType,
                    IsNullable = col.IsNullable,
                    Ordinal = col.Ordinal,
                    MaxLength = col.MaxLength,
                    Precision = col.Precision,
                    Scale = col.Scale,
                    IsPrimaryKey = true,
                    IsIdentity = col.IsIdentity,
                    IsComputed = col.IsComputed,
                    DefaultValue = col.DefaultValue,
                    Description = col.Description
                });
            }
            else
            {
                result.Add(col);
            }
        }

        return result;
    }

    private static bool MatchesLikePattern(string value, string likePattern)
    {
        try
        {
            // Convert SQL LIKE pattern to regex
            var regexPattern = "^" + Regex.Escape(likePattern)
                .Replace("%", ".*", StringComparison.Ordinal)
                .Replace("_", ".", StringComparison.Ordinal) + "$";

            return Regex.IsMatch(value, regexPattern, RegexOptions.IgnoreCase, RegexTimeout);
        }
        catch (RegexMatchTimeoutException ex)
        {
            // Why: regex DoS protection — return false (no match) on timeout; the exception is
            // observed via ex to satisfy FDW022. No logger is available in this static method.
            _ = ex;
            return false;
        }
    }

    /// <summary>
    /// Maps PostgreSQL foreign key action codes to human-readable strings.
    /// </summary>
    private static string MapForeignKeyAction(char actionCode)
    {
        return actionCode switch
        {
            'a' => "NO ACTION",
            'r' => "RESTRICT",
            'c' => "CASCADE",
            'n' => "SET NULL",
            'd' => "SET DEFAULT",
            _ => "NO ACTION"
        };
    }

    #endregion

    #region TableKeyComparer

    /// <summary>
    /// Case-insensitive comparer for (Schema, Table) tuples.
    /// </summary>
    private sealed class TableKeyComparer : IEqualityComparer<(string Schema, string Table)>
    {
        public static readonly TableKeyComparer Instance = new();

        public bool Equals((string Schema, string Table) x, (string Schema, string Table) y)
            => string.Equals(x.Schema, y.Schema, StringComparison.OrdinalIgnoreCase)
               && string.Equals(x.Table, y.Table, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string Schema, string Table) obj)
            => HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Schema),
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Table));
    }

    #endregion
}
