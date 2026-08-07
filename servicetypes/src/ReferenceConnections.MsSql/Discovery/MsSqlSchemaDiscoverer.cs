using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Conventions;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections.MsSql.Logging;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Fdw.Services.Connections.MsSql;
using Fdw.Services.Connections.MsSql.Discovery;

using ReferenceConnections.MsSql;

using ReferenceConnections.MsSql.Discovery;

namespace ReferenceConnections.MsSql.Discovery;

/// <summary>
/// Implementation of schema discovery for SQL Server databases.
/// Queries INFORMATION_SCHEMA and system tables to discover tables, views, columns, and indexes.
/// Uses batched queries to minimize round trips — 5-8 queries total regardless of table count.
/// </summary>
[ExcludeFromCodeCoverage] // Excluded: requires SQL Server connection
public sealed class MsSqlSchemaDiscoverer : IMsSqlSchemaDiscoverer
{
    private readonly ILogger<MsSqlSchemaDiscoverer> _logger;

    // Compiled regex for LIKE pattern matching with timeout for DoS protection
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    #region SQL Queries

    private const string DatabaseNameQuery = "SELECT DB_NAME()";

    private const string SchemasQuery = @"
        SELECT SCHEMA_NAME
        FROM INFORMATION_SCHEMA.SCHEMATA
        WHERE SCHEMA_NAME NOT IN ('sys', 'INFORMATION_SCHEMA', 'guest')
        ORDER BY SCHEMA_NAME";

    private const string AllTablesQuery = @"
        SELECT TABLE_SCHEMA, TABLE_NAME, TABLE_TYPE
        FROM INFORMATION_SCHEMA.TABLES
        WHERE TABLE_SCHEMA NOT IN ('sys', 'INFORMATION_SCHEMA', 'guest')
        ORDER BY TABLE_SCHEMA, TABLE_NAME";

    private const string AllColumnsQuery = @"
        SELECT
            c.TABLE_SCHEMA, c.TABLE_NAME,
            c.COLUMN_NAME, c.DATA_TYPE, c.IS_NULLABLE,
            c.CHARACTER_MAXIMUM_LENGTH, c.NUMERIC_PRECISION, c.NUMERIC_SCALE,
            c.COLUMN_DEFAULT, c.ORDINAL_POSITION,
            COLUMNPROPERTY(OBJECT_ID(QUOTENAME(c.TABLE_SCHEMA) + '.' + QUOTENAME(c.TABLE_NAME)), c.COLUMN_NAME, 'IsIdentity') AS IsIdentity,
            COLUMNPROPERTY(OBJECT_ID(QUOTENAME(c.TABLE_SCHEMA) + '.' + QUOTENAME(c.TABLE_NAME)), c.COLUMN_NAME, 'IsComputed') AS IsComputed,
            CAST(ep.value AS NVARCHAR(4000)) AS Description
        FROM INFORMATION_SCHEMA.COLUMNS c
        LEFT JOIN sys.extended_properties ep
            ON ep.major_id = OBJECT_ID(QUOTENAME(c.TABLE_SCHEMA) + '.' + QUOTENAME(c.TABLE_NAME))
            AND ep.minor_id = c.ORDINAL_POSITION
            AND ep.name = 'MS_Description'
        WHERE c.TABLE_SCHEMA NOT IN ('sys', 'INFORMATION_SCHEMA', 'guest')
        ORDER BY c.TABLE_SCHEMA, c.TABLE_NAME, c.ORDINAL_POSITION";

    private const string AllPrimaryKeysQuery = @"
        SELECT tc.TABLE_SCHEMA, tc.TABLE_NAME, kcu.COLUMN_NAME, kcu.ORDINAL_POSITION
        FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
        JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
            ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
            AND tc.TABLE_SCHEMA = kcu.TABLE_SCHEMA
        WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
            AND tc.TABLE_SCHEMA NOT IN ('sys', 'INFORMATION_SCHEMA', 'guest')
        ORDER BY tc.TABLE_SCHEMA, tc.TABLE_NAME, kcu.ORDINAL_POSITION";

    private const string AllIndexesQuery = @"
        SELECT s.name AS SchemaName, t.name AS TableName,
            i.name AS IndexName, c.name AS ColumnName,
            i.is_unique, i.is_primary_key, i.type_desc, ic.key_ordinal
        FROM sys.indexes i
        JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
        JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
        JOIN sys.tables t ON i.object_id = t.object_id
        JOIN sys.schemas s ON t.schema_id = s.schema_id
        WHERE i.name IS NOT NULL
            AND s.name NOT IN ('sys', 'INFORMATION_SCHEMA', 'guest')
        ORDER BY s.name, t.name, i.name, ic.key_ordinal";

    private const string AllForeignKeysQuery = @"
        SELECT s.name AS SchemaName, t.name AS TableName,
            fk.name AS ForeignKeyName, c.name AS ColumnName,
            SCHEMA_NAME(rt.schema_id) AS ReferencedSchema,
            OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable,
            rc.name AS ReferencedColumn,
            fk.delete_referential_action, fk.update_referential_action,
            fk.is_disabled, fkc.constraint_column_id
        FROM sys.foreign_keys fk
        JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
        JOIN sys.columns c ON fkc.parent_object_id = c.object_id AND fkc.parent_column_id = c.column_id
        JOIN sys.columns rc ON fkc.referenced_object_id = rc.object_id AND fkc.referenced_column_id = rc.column_id
        JOIN sys.tables rt ON fk.referenced_object_id = rt.object_id
        JOIN sys.tables t ON fk.parent_object_id = t.object_id
        JOIN sys.schemas s ON t.schema_id = s.schema_id
        WHERE s.name NOT IN ('sys', 'INFORMATION_SCHEMA', 'guest')
        ORDER BY s.name, t.name, fk.name, fkc.constraint_column_id";

    private const string AllDescriptionsQuery = @"
        SELECT s.name AS SchemaName, t.name AS TableName,
            CAST(ep.value AS NVARCHAR(4000)) AS Description
        FROM sys.extended_properties ep
        JOIN sys.tables t ON ep.major_id = t.object_id
        JOIN sys.schemas s ON t.schema_id = s.schema_id
        WHERE ep.name = 'MS_Description' AND ep.minor_id = 0
            AND s.name NOT IN ('sys', 'INFORMATION_SCHEMA', 'guest')";

    // Per-table queries used by DiscoverContainer (single container discovery)
    private const string SingleTableColumnsQuery = @"
        SELECT
            c.COLUMN_NAME, c.DATA_TYPE, c.IS_NULLABLE,
            c.CHARACTER_MAXIMUM_LENGTH, c.NUMERIC_PRECISION, c.NUMERIC_SCALE,
            c.COLUMN_DEFAULT, c.ORDINAL_POSITION,
            COLUMNPROPERTY(OBJECT_ID(QUOTENAME(c.TABLE_SCHEMA) + '.' + QUOTENAME(c.TABLE_NAME)), c.COLUMN_NAME, 'IsIdentity') AS IsIdentity,
            COLUMNPROPERTY(OBJECT_ID(QUOTENAME(c.TABLE_SCHEMA) + '.' + QUOTENAME(c.TABLE_NAME)), c.COLUMN_NAME, 'IsComputed') AS IsComputed,
            CAST(ep.value AS NVARCHAR(4000)) AS Description
        FROM INFORMATION_SCHEMA.COLUMNS c
        LEFT JOIN sys.extended_properties ep
            ON ep.major_id = OBJECT_ID(QUOTENAME(c.TABLE_SCHEMA) + '.' + QUOTENAME(c.TABLE_NAME))
            AND ep.minor_id = c.ORDINAL_POSITION
            AND ep.name = 'MS_Description'
        WHERE c.TABLE_SCHEMA = @SchemaName AND c.TABLE_NAME = @ObjectName
        ORDER BY c.ORDINAL_POSITION";

    private const string SingleTablePrimaryKeyQuery = @"
        SELECT kcu.COLUMN_NAME
        FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
        JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
            ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
            AND tc.TABLE_SCHEMA = kcu.TABLE_SCHEMA
            AND tc.TABLE_NAME = kcu.TABLE_NAME
        WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
            AND tc.TABLE_SCHEMA = @SchemaName
            AND tc.TABLE_NAME = @ObjectName
        ORDER BY kcu.ORDINAL_POSITION";

    private const string SingleTableIndexesQuery = @"
        SELECT
            i.name AS IndexName, c.name AS ColumnName,
            i.is_unique AS IsUnique, i.is_primary_key AS IsPrimaryKey,
            i.type_desc AS TypeDesc
        FROM sys.indexes i
        JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
        JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
        JOIN sys.tables t ON i.object_id = t.object_id
        JOIN sys.schemas s ON t.schema_id = s.schema_id
        WHERE s.name = @SchemaName AND t.name = @ObjectName AND i.name IS NOT NULL
        ORDER BY i.name, ic.key_ordinal";

    private const string SingleTableObjectTypeQuery = @"
        SELECT TABLE_TYPE
        FROM INFORMATION_SCHEMA.TABLES
        WHERE TABLE_SCHEMA = @SchemaName AND TABLE_NAME = @ObjectName";

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlSchemaDiscoverer"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public MsSqlSchemaDiscoverer(ILogger<MsSqlSchemaDiscoverer>? logger = null)
    {
        _logger = logger ?? NullLogger<MsSqlSchemaDiscoverer>.Instance;
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Method is too long - sequential schema discovery with batched queries and in-memory assembly
    public async Task<IGenericResult<SchemaDiscoveryResult>> DiscoverSchema(
        MsSqlConnection connection,
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
                    MsSqlSchemaDiscoveryLog.ConnectionFailed(_logger, connectResult.CurrentMessage ?? "Unknown error"));
            }

            // Use a single pooled connection for the entire discovery session
            using var sqlConnection = CreateSqlConnection(connection);
            await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);

            // 1. Get database name
            var databaseName = await GetDatabaseName(sqlConnection, cancellationToken).ConfigureAwait(false);
            MsSqlSchemaDiscoveryLog.SchemaDiscoveryStarted(_logger, databaseName);

            // 2. Discover and filter schemas
            var schemas = await DiscoverSchemasInternal(sqlConnection, options, cancellationToken).ConfigureAwait(false);
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

            MsSqlSchemaDiscoveryLog.SchemasDiscovered(_logger, schemas.Count);

            // 3. Batch query: ALL tables/views across ALL schemas
            var allTables = await DiscoverAllTablesInternal(sqlConnection, options, schemas, cancellationToken).ConfigureAwait(false);
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
            var allColumns = await DiscoverAllColumnsInternal(sqlConnection, tableKeys, cancellationToken).ConfigureAwait(false);

            // 5. Batch query: ALL primary keys across ALL tables
            var allPrimaryKeys = await DiscoverAllPrimaryKeysInternal(sqlConnection, tableKeys, cancellationToken).ConfigureAwait(false);

            // 6. Batch query: ALL indexes (optional)
            var allIndexes = options.DiscoverIndexes
                ? await DiscoverAllIndexesInternal(sqlConnection, tableKeys, cancellationToken).ConfigureAwait(false)
                : new Dictionary<(string Schema, string Table), List<DiscoveredIndex>>(TableKeyComparer.Instance);

            // 7. Batch query: ALL foreign keys (optional)
            var allForeignKeys = options.DiscoverForeignKeys
                ? await DiscoverAllForeignKeysInternal(sqlConnection, tableKeys, cancellationToken).ConfigureAwait(false)
                : new Dictionary<(string Schema, string Table), List<DiscoveredForeignKey>>(TableKeyComparer.Instance);

            // 8. Batch query: ALL descriptions (optional)
            var allDescriptions = options.DiscoverDescriptions
                ? await DiscoverAllDescriptionsInternal(sqlConnection, tableKeys, cancellationToken).ConfigureAwait(false)
                : new Dictionary<(string Schema, string Table), string>(TableKeyComparer.Instance);

            // Assemble results: group tables by schema, build DiscoveredContainer for each
            var result = AssembleDiscoveryResult(
                databaseName, allTables, allColumns, allPrimaryKeys, allIndexes, allForeignKeys, allDescriptions);

            MsSqlSchemaDiscoveryLog.SchemaDiscoveryCompleted(_logger, result.Paths.Count, result.TotalContainers, result.TotalFields);

            return GenericResult<SchemaDiscoveryResult>.Success(result);
        }
        catch (Exception ex)
        {
            return GenericResult<SchemaDiscoveryResult>.Failure(
                MsSqlSchemaDiscoveryLog.SchemaDiscoveryFailed(_logger, ex.Message));
        }
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Method is too long - sequential container discovery and field mapping, not complex
    public async Task<IGenericResult<DiscoveredContainer>> DiscoverContainer(
        MsSqlConnection connection,
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
                    MsSqlSchemaDiscoveryLog.ConnectionFailed(_logger, connectResult.CurrentMessage ?? "Unknown error"));
            }

            MsSqlSchemaDiscoveryLog.ContainerDiscoveryStarted(_logger, schemaName, objectName);

            // Use a single connection for the entire single-container discovery
            using var sqlConnection = CreateSqlConnection(connection);
            await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);

            // Get object type
            var objectType = await GetObjectType(sqlConnection, schemaName, objectName, cancellationToken).ConfigureAwait(false);
            if (objectType == null)
            {
                return GenericResult<DiscoveredContainer>.Failure(
                    MsSqlSchemaDiscoveryLog.ContainerNotFound(_logger, schemaName, objectName));
            }

            // Discover columns
            var columns = await DiscoverSingleTableColumns(sqlConnection, schemaName, objectName, cancellationToken).ConfigureAwait(false);

            // Discover primary key
            var primaryKeyColumns = await DiscoverSingleTablePrimaryKey(sqlConnection, schemaName, objectName, cancellationToken).ConfigureAwait(false);

            // Mark primary key columns
            var fields = MarkPrimaryKeyFields(columns, primaryKeyColumns);

            // Discover indexes
            var indexes = await DiscoverSingleTableIndexes(sqlConnection, schemaName, objectName, cancellationToken).ConfigureAwait(false);

            var container = new DiscoveredContainer
            {
                Name = objectName,
                ContainerType = objectType,
                Fields = fields,
                PrimaryKeyColumns = primaryKeyColumns,
                Indexes = indexes
            };
            container.SchemaName = schemaName;

            MsSqlSchemaDiscoveryLog.ContainerDiscovered(_logger, objectType, schemaName, objectName, fields.Count);

            return GenericResult<DiscoveredContainer>.Success(container);
        }
        catch (Exception ex)
        {
            return GenericResult<DiscoveredContainer>.Failure(
                MsSqlSchemaDiscoveryLog.ContainerDiscoveryFailed(_logger, schemaName, objectName, ex.Message));
        }
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<bool>> TestDiscoveryCapability(
        MsSqlConnection connection,
        CancellationToken cancellationToken)
    {
        try
        {
            var connectResult = await connection.Connect(cancellationToken).ConfigureAwait(false);
            if (!connectResult.IsSuccess)
            {
                return GenericResult<bool>.Failure(
                    MsSqlSchemaDiscoveryLog.ConnectionFailed(_logger, connectResult.CurrentMessage ?? "Unknown error"));
            }

            // Test by querying INFORMATION_SCHEMA
            const string testQuery = "SELECT TOP 1 SCHEMA_NAME FROM INFORMATION_SCHEMA.SCHEMATA";
            using var sqlConnection = CreateSqlConnection(connection);
            await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = new SqlCommand(testQuery, sqlConnection);
            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

            return GenericResult<bool>.Success(result != null);
        }
        catch (Exception ex)
        {
            return GenericResult<bool>.Failure(MsSqlConnectionLogger.DiscoveryTestFailed(_logger, ex.Message));
        }
    }

    #region Batched Discovery — DiscoverSchema internals

    private static async Task<string> GetDatabaseName(SqlConnection sqlConnection, CancellationToken cancellationToken)
    {
        using var command = new SqlCommand(DatabaseNameQuery, sqlConnection);
        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return result?.ToString() ?? "Unknown";
    }

    private static async Task<List<string>> DiscoverSchemasInternal(
        SqlConnection sqlConnection,
        SchemaDiscoveryOptions options,
        CancellationToken cancellationToken)
    {
        var schemas = new List<string>();
        using var command = new SqlCommand(SchemasQuery, sqlConnection);
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
        SqlConnection sqlConnection,
        SchemaDiscoveryOptions options,
        List<string> filteredSchemas,
        CancellationToken cancellationToken)
    {
        var schemaSet = new HashSet<string>(filteredSchemas, StringComparer.OrdinalIgnoreCase);
        var perSchemaCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var tables = new List<(string Schema, string Table, string ContainerType)>();

        using var command = new SqlCommand(AllTablesQuery, sqlConnection);
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

            // Skip system tables if not included
            if (!options.IncludeSystemTables && tableName.StartsWith("sys", StringComparison.OrdinalIgnoreCase))
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

    [ConventionOverride(MaxCyclomaticComplexity = 15)]
    private static async Task<Dictionary<(string Schema, string Table), List<DiscoveredField>>> DiscoverAllColumnsInternal(
        SqlConnection sqlConnection,
        HashSet<(string Schema, string Table)> tableKeys,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<(string Schema, string Table), List<DiscoveredField>>(TableKeyComparer.Instance);

        using var command = new SqlCommand(AllColumnsQuery, sqlConnection);
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
                Precision = await reader.IsDBNullAsync(6, cancellationToken).ConfigureAwait(false) ? null : reader.GetByte(6),
                Scale = await reader.IsDBNullAsync(7, cancellationToken).ConfigureAwait(false) ? null : reader.GetInt32(7),
                DefaultValue = await reader.IsDBNullAsync(8, cancellationToken).ConfigureAwait(false) ? null : reader.GetString(8),
                Ordinal = reader.GetInt32(9),
                IsIdentity = !await reader.IsDBNullAsync(10, cancellationToken).ConfigureAwait(false) && reader.GetInt32(10) == 1,
                IsComputed = !await reader.IsDBNullAsync(11, cancellationToken).ConfigureAwait(false) && reader.GetInt32(11) == 1,
                IsPrimaryKey = false, // Will be set during assembly
                Description = await reader.IsDBNullAsync(12, cancellationToken).ConfigureAwait(false) ? null : reader.GetString(12)
            };

            if (!result.TryGetValue(key, out var fields))
            {
                fields = [];
                result[key] = fields;
            }

            fields.Add(field);
        }

        return result;
    }

    private static async Task<Dictionary<(string Schema, string Table), List<string>>> DiscoverAllPrimaryKeysInternal(
        SqlConnection sqlConnection,
        HashSet<(string Schema, string Table)> tableKeys,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<(string Schema, string Table), List<string>>(TableKeyComparer.Instance);

        using var command = new SqlCommand(AllPrimaryKeysQuery, sqlConnection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var schema = reader.GetString(0);
            var table = reader.GetString(1);
            var columnName = reader.GetString(2);
            var key = (schema, table);

            if (!tableKeys.Contains(key))
                continue;

            if (!result.TryGetValue(key, out var columns))
            {
                columns = [];
                result[key] = columns;
            }

            columns.Add(columnName);
        }

        return result;
    }

    private static async Task<Dictionary<(string Schema, string Table), List<DiscoveredIndex>>> DiscoverAllIndexesInternal(
        SqlConnection sqlConnection,
        HashSet<(string Schema, string Table)> tableKeys,
        CancellationToken cancellationToken)
    {
        // Intermediate: group by (schema, table, indexName)
        var indexDict = new Dictionary<(string Schema, string Table, string IndexName), (List<string> Columns, bool IsUnique, bool IsPrimaryKey, bool IsClustered)>(
            SchemaTableIndexKeyComparer.Instance);

        using var command = new SqlCommand(AllIndexesQuery, sqlConnection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var schema = reader.GetString(0);
            var table = reader.GetString(1);
            var indexName = reader.GetString(2);
            var columnName = reader.GetString(3);
            var isUnique = reader.GetBoolean(4);
            var isPrimaryKey = reader.GetBoolean(5);
            var typeDesc = reader.GetString(6);
            var tableKey = (schema, table);

            if (!tableKeys.Contains(tableKey))
                continue;

            var indexKey = (schema, table, indexName);
            var isClustered = string.Equals(typeDesc, "CLUSTERED", StringComparison.OrdinalIgnoreCase);

            if (!indexDict.TryGetValue(indexKey, out var indexInfo))
            {
                indexInfo = ([], isUnique, isPrimaryKey, isClustered);
                indexDict[indexKey] = indexInfo;
            }

            indexInfo.Columns.Add(columnName);
        }

        // Flatten into per-table dictionary
        var result = new Dictionary<(string Schema, string Table), List<DiscoveredIndex>>(TableKeyComparer.Instance);

        foreach (var kvp in indexDict)
        {
            var tableKey = (kvp.Key.Schema, kvp.Key.Table);
            if (!result.TryGetValue(tableKey, out var indexes))
            {
                indexes = [];
                result[tableKey] = indexes;
            }

            indexes.Add(new DiscoveredIndex
            {
                Name = kvp.Key.IndexName,
                Columns = kvp.Value.Columns,
                IsUnique = kvp.Value.IsUnique,
                IsPrimaryKey = kvp.Value.IsPrimaryKey,
                IsClustered = kvp.Value.IsClustered
            });
        }

        return result;
    }

    [ConventionOverride(MaxCyclomaticComplexity = 15)]
    private static async Task<Dictionary<(string Schema, string Table), List<DiscoveredForeignKey>>> DiscoverAllForeignKeysInternal(
        SqlConnection sqlConnection,
        HashSet<(string Schema, string Table)> tableKeys,
        CancellationToken cancellationToken)
    {
        // Intermediate: group by (schema, table, fkName)
        var fkDict = new Dictionary<(string Schema, string Table, string FkName), (List<DiscoveredForeignKeyColumn> Columns, string RefSchema, string RefTable, int OnDelete, int OnUpdate, bool IsEnabled)>(
            SchemaTableIndexKeyComparer.Instance);

        using var command = new SqlCommand(AllForeignKeysQuery, sqlConnection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var schema = reader.GetString(0);
            var table = reader.GetString(1);
            var fkName = reader.GetString(2);
            var columnName = reader.GetString(3);
            var refSchema = reader.GetString(4);
            var refTable = reader.GetString(5);
            var refColumn = reader.GetString(6);
            var onDelete = reader.GetByte(7);
            var onUpdate = reader.GetByte(8);
            var isDisabled = reader.GetBoolean(9);
            var ordinal = reader.GetInt32(10);
            var tableKey = (schema, table);

            if (!tableKeys.Contains(tableKey))
                continue;

            var fkKey = (schema, table, fkName);

            if (!fkDict.TryGetValue(fkKey, out var fkInfo))
            {
                fkInfo = ([], refSchema, refTable, onDelete, onUpdate, !isDisabled);
                fkDict[fkKey] = fkInfo;
            }

            fkInfo.Columns.Add(new DiscoveredForeignKeyColumn
            {
                ColumnName = columnName,
                ReferencedColumnName = refColumn,
                Ordinal = ordinal
            });
        }

        // Flatten into per-table dictionary
        var result = new Dictionary<(string Schema, string Table), List<DiscoveredForeignKey>>(TableKeyComparer.Instance);

        foreach (var kvp in fkDict)
        {
            var tableKey = (kvp.Key.Schema, kvp.Key.Table);
            if (!result.TryGetValue(tableKey, out var foreignKeys))
            {
                foreignKeys = [];
                result[tableKey] = foreignKeys;
            }

            foreignKeys.Add(new DiscoveredForeignKey
            {
                Name = kvp.Key.FkName,
                Columns = kvp.Value.Columns,
                ReferencedSchema = kvp.Value.RefSchema,
                ReferencedTable = kvp.Value.RefTable,
                OnDelete = MapReferentialAction(kvp.Value.OnDelete),
                OnUpdate = MapReferentialAction(kvp.Value.OnUpdate),
                IsEnabled = kvp.Value.IsEnabled
            });
        }

        return result;
    }

    private static async Task<Dictionary<(string Schema, string Table), string>> DiscoverAllDescriptionsInternal(
        SqlConnection sqlConnection,
        HashSet<(string Schema, string Table)> tableKeys,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<(string Schema, string Table), string>(TableKeyComparer.Instance);

        using var command = new SqlCommand(AllDescriptionsQuery, sqlConnection);
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

    private static SchemaDiscoveryResult AssembleDiscoveryResult(
        string databaseName,
        List<(string Schema, string Table, string ContainerType)> allTables,
        Dictionary<(string Schema, string Table), List<DiscoveredField>> allColumns,
        Dictionary<(string Schema, string Table), List<string>> allPrimaryKeys,
        Dictionary<(string Schema, string Table), List<DiscoveredIndex>> allIndexes,
        Dictionary<(string Schema, string Table), List<DiscoveredForeignKey>> allForeignKeys,
        Dictionary<(string Schema, string Table), string> allDescriptions)
    {
        // Group tables by schema
        var schemaGroups = new Dictionary<string, List<(string Table, string ContainerType)>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (schema, table, containerType) in allTables)
        {
            if (!schemaGroups.TryGetValue(schema, out var group))
            {
                group = [];
                schemaGroups[schema] = group;
            }
            group.Add((table, containerType));
        }

        var paths = new List<DiscoveredPath>();
        var totalContainers = 0;
        var totalFields = 0;

        foreach (var kvp in schemaGroups)
        {
            var schemaName = kvp.Key;
            var containers = new List<DiscoveredContainer>();

            foreach (var (tableName, containerType) in kvp.Value)
            {
                var key = (schemaName, tableName);

                // Get columns for this table
                var columns = allColumns.TryGetValue(key, out var cols) ? cols : [];

                // Get primary key columns for this table
                var pkColumns = allPrimaryKeys.TryGetValue(key, out var pks) ? pks : [];

                // Mark PK fields
                var fields = MarkPrimaryKeyFields(columns, pkColumns);

                // Get indexes
                var indexes = allIndexes.TryGetValue(key, out var idx) ? (IReadOnlyList<DiscoveredIndex>)idx : [];

                // Get foreign keys (only for tables, not views)
                IReadOnlyList<DiscoveredForeignKey> foreignKeys = string.Equals(containerType, "Table", StringComparison.OrdinalIgnoreCase)
                    && allForeignKeys.TryGetValue(key, out var fks)
                    ? fks
                    : [];

                // Get description
                allDescriptions.TryGetValue(key, out var description);

                var container = new DiscoveredContainer
                {
                    Name = tableName,
                    ContainerType = containerType,
                    Fields = fields,
                    PrimaryKeyColumns = pkColumns,
                    Indexes = indexes,
                    ForeignKeys = foreignKeys,
                    Description = description
                };
                container.SchemaName = schemaName;

                containers.Add(container);
                totalContainers++;
                totalFields += fields.Count;
            }

            if (containers.Count > 0)
            {
                paths.Add(new DiscoveredPath
                {
                    SchemaName = schemaName,
                    Containers = containers
                });
            }
        }

        return new SchemaDiscoveryResult
        {
            DatabaseName = databaseName,
            Paths = paths,
            TotalContainers = totalContainers,
            TotalFields = totalFields
        };
    }

    #endregion

    #region Single Container Discovery — DiscoverContainer internals

    private static async Task<string?> GetObjectType(
        SqlConnection sqlConnection,
        string schemaName,
        string objectName,
        CancellationToken cancellationToken)
    {
        using var command = new SqlCommand(SingleTableObjectTypeQuery, sqlConnection);
        command.Parameters.AddWithValue("@SchemaName", schemaName);
        command.Parameters.AddWithValue("@ObjectName", objectName);

        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        if (result == null)
        {
            return null;
        }

        var type = result.ToString();
        return string.Equals(type, "VIEW", StringComparison.OrdinalIgnoreCase) ? "View" : "Table";
    }

    [ConventionOverride(MaxCyclomaticComplexity = 15)]
    private static async Task<List<DiscoveredField>> DiscoverSingleTableColumns(
        SqlConnection sqlConnection,
        string schemaName,
        string objectName,
        CancellationToken cancellationToken)
    {
        var fields = new List<DiscoveredField>();

        using var command = new SqlCommand(SingleTableColumnsQuery, sqlConnection);
        command.Parameters.AddWithValue("@SchemaName", schemaName);
        command.Parameters.AddWithValue("@ObjectName", objectName);

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            fields.Add(new DiscoveredField
            {
                Name = reader.GetString(0),
                SqlType = reader.GetString(1),
                IsNullable = string.Equals(reader.GetString(2), "YES", StringComparison.OrdinalIgnoreCase),
                MaxLength = await reader.IsDBNullAsync(3, cancellationToken).ConfigureAwait(false) ? null : reader.GetInt32(3),
                Precision = await reader.IsDBNullAsync(4, cancellationToken).ConfigureAwait(false) ? null : reader.GetByte(4),
                Scale = await reader.IsDBNullAsync(5, cancellationToken).ConfigureAwait(false) ? null : reader.GetInt32(5),
                DefaultValue = await reader.IsDBNullAsync(6, cancellationToken).ConfigureAwait(false) ? null : reader.GetString(6),
                Ordinal = reader.GetInt32(7),
                IsIdentity = !await reader.IsDBNullAsync(8, cancellationToken).ConfigureAwait(false) && reader.GetInt32(8) == 1,
                IsComputed = !await reader.IsDBNullAsync(9, cancellationToken).ConfigureAwait(false) && reader.GetInt32(9) == 1,
                IsPrimaryKey = false, // Will be set later
                Description = await reader.IsDBNullAsync(10, cancellationToken).ConfigureAwait(false) ? null : reader.GetString(10)
            });
        }

        return fields;
    }

    private static async Task<List<string>> DiscoverSingleTablePrimaryKey(
        SqlConnection sqlConnection,
        string schemaName,
        string objectName,
        CancellationToken cancellationToken)
    {
        var columns = new List<string>();

        using var command = new SqlCommand(SingleTablePrimaryKeyQuery, sqlConnection);
        command.Parameters.AddWithValue("@SchemaName", schemaName);
        command.Parameters.AddWithValue("@ObjectName", objectName);

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            columns.Add(reader.GetString(0));
        }

        return columns;
    }

    private static async Task<List<DiscoveredIndex>> DiscoverSingleTableIndexes(
        SqlConnection sqlConnection,
        string schemaName,
        string objectName,
        CancellationToken cancellationToken)
    {
        var indexDict = new Dictionary<string, (List<string> Columns, bool IsUnique, bool IsPrimaryKey, bool IsClustered)>(StringComparer.OrdinalIgnoreCase);

        using var command = new SqlCommand(SingleTableIndexesQuery, sqlConnection);
        command.Parameters.AddWithValue("@SchemaName", schemaName);
        command.Parameters.AddWithValue("@ObjectName", objectName);

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var indexName = reader.GetString(0);
            var columnName = reader.GetString(1);
            var isUnique = reader.GetBoolean(2);
            var isPrimaryKey = reader.GetBoolean(3);
            var typeDesc = reader.GetString(4);
            var isClustered = string.Equals(typeDesc, "CLUSTERED", StringComparison.OrdinalIgnoreCase);

            if (!indexDict.TryGetValue(indexName, out var indexInfo))
            {
                indexInfo = ([], isUnique, isPrimaryKey, isClustered);
                indexDict[indexName] = indexInfo;
            }

            indexInfo.Columns.Add(columnName);
        }

        var indexes = new List<DiscoveredIndex>();
        foreach (var kvp in indexDict)
        {
            indexes.Add(new DiscoveredIndex
            {
                Name = kvp.Key,
                Columns = kvp.Value.Columns,
                IsUnique = kvp.Value.IsUnique,
                IsPrimaryKey = kvp.Value.IsPrimaryKey,
                IsClustered = kvp.Value.IsClustered
            });
        }

        return indexes;
    }

    #endregion

    #region Shared Helpers

    private static List<DiscoveredField> MarkPrimaryKeyFields(
        List<DiscoveredField> columns,
        List<string> primaryKeyColumns)
    {
        if (primaryKeyColumns.Count == 0)
        {
            return columns;
        }

        var fields = new List<DiscoveredField>(columns.Count);
        foreach (var field in columns)
        {
            if (primaryKeyColumns.Contains(field.Name, StringComparer.OrdinalIgnoreCase))
            {
                fields.Add(new DiscoveredField
                {
                    Name = field.Name,
                    SqlType = field.SqlType,
                    IsNullable = field.IsNullable,
                    Ordinal = field.Ordinal,
                    MaxLength = field.MaxLength,
                    Precision = field.Precision,
                    Scale = field.Scale,
                    IsPrimaryKey = true,
                    IsIdentity = field.IsIdentity,
                    IsComputed = field.IsComputed,
                    DefaultValue = field.DefaultValue,
                    Description = field.Description
                });
            }
            else
            {
                fields.Add(field);
            }
        }

        return fields;
    }

    private static IForeignKeyAction MapReferentialAction(int action) => action switch
    {
        0 => new NoActionForeignKeyAction(),
        1 => new CascadeForeignKeyAction(),
        2 => new SetNullForeignKeyAction(),
        3 => new SetDefaultForeignKeyAction(),
        _ => new NoActionForeignKeyAction()
    };

    private static SqlConnection CreateSqlConnection(MsSqlConnection connection)
    {
        return connection.CreatePooledConnection();
    }

    private static bool MatchesLikePattern(string value, string pattern)
    {
        // Simple LIKE pattern matching (supports % and _ wildcards)
        // Using compiled regex with timeout for DoS protection (MA0009)
        try
        {
            var regexPattern = "^" + Regex.Escape(pattern)
                .Replace("%", ".*", StringComparison.Ordinal)
                .Replace("_", ".", StringComparison.Ordinal) + "$";

            return Regex.IsMatch(value, regexPattern, RegexOptions.IgnoreCase, RegexTimeout);
        }
        catch (RegexMatchTimeoutException ex)
        {
            // Why: regex DoS protection — fall back to a simple string check on timeout;
            // the exception is observed via ex to satisfy FDW022, but there is no meaningful
            // additional detail to surface since a timeout is the expected/only reason this fires.
            _ = ex;
            return pattern.Contains('%')
                ? value.Contains(pattern.Replace("%", string.Empty, StringComparison.Ordinal), StringComparison.OrdinalIgnoreCase)
                : string.Equals(value, pattern, StringComparison.OrdinalIgnoreCase);
        }
    }

    #endregion

    #region Equality Comparers

    /// <summary>
    /// Case-insensitive comparer for (Schema, Table) tuple keys.
    /// </summary>
    private sealed class TableKeyComparer : IEqualityComparer<(string Schema, string Table)>
    {
        public static readonly TableKeyComparer Instance = new();

        public bool Equals((string Schema, string Table) x, (string Schema, string Table) y)
        {
            return string.Equals(x.Schema, y.Schema, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.Table, y.Table, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode((string Schema, string Table) obj)
        {
            return HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Schema),
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Table));
        }
    }

    /// <summary>
    /// Case-insensitive comparer for (Schema, Table, Name) tuple keys used for index and FK grouping.
    /// </summary>
    private sealed class SchemaTableIndexKeyComparer : IEqualityComparer<(string Schema, string Table, string Name)>
    {
        public static readonly SchemaTableIndexKeyComparer Instance = new();

        public bool Equals((string Schema, string Table, string Name) x, (string Schema, string Table, string Name) y)
        {
            return string.Equals(x.Schema, y.Schema, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.Table, y.Table, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode((string Schema, string Table, string Name) obj)
        {
            return HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Schema),
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Table),
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Name));
        }
    }

    #endregion
}
