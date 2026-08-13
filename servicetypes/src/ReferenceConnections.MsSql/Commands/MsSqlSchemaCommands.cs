using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Conventions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Commands;
using Fdw.Services.Connections.MsSql.Discovery;
using Fdw.Services.Connections.MsSql.Logging;
using Fdw.Services.Data;
using Microsoft.Data.SqlClient;
using SchemaDiscoveryOptions = Fdw.Services.Connections.MsSql.Discovery.SchemaDiscoveryOptions;
using SchemaDiscoveryResult = Fdw.Services.Connections.MsSql.Discovery.SchemaDiscoveryResult;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using ReferenceConnections.MsSql;

namespace Fdw.Services.Connections.MsSql.Commands;

/// <summary>
/// Static commands for SQL Server schema discovery and persistence.
/// Stateless operations that can work with either SqlConnection or MsSqlConnection.
/// </summary>
[ExcludeFromCodeCoverage] // Excluded: requires SQL Server connection
public static class MsSqlSchemaCommands
{
    #region Discover Schema (Full Database)

    /// <summary>
    /// Discovers the schema of the entire database using a raw SqlConnection.
    /// </summary>
    /// <param name="connection">An open SqlConnection.</param>
    /// <param name="options">Discovery options (filters, etc.).</param>
    /// <param name="logger">Optional logger. Uses NullLogger if not provided.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The discovered schema result.</returns>
    public static async Task<IGenericResult<SchemaDiscoveryResult>> DiscoverSchema(
        SqlConnection connection,
        SchemaDiscoveryOptions? options = null,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        logger ??= NullLogger.Instance;
        options ??= SchemaDiscoveryOptions.Default;

        try
        {
            if (connection.State != ConnectionState.Open)
            {
                return GenericResult<SchemaDiscoveryResult>.Failure(
                    MsSqlSchemaCommandsLog.ConnectionNotOpen(logger));
            }

            var databaseName = connection.Database;
            MsSqlSchemaCommandsLog.SchemaDiscoveryStarted(logger, databaseName);

            // Discover schemas
            var schemas = await DiscoverSchemasInternal(connection, options, logger, cancellationToken).ConfigureAwait(false);
            if (!schemas.IsSuccess || schemas.Value == null)
            {
                return schemas.IsSuccess
                    ? GenericResult<SchemaDiscoveryResult>.Failure(
                        MsSqlSchemaCommandsLog.SchemaDiscoveryFailed(logger, "Failed to discover schemas"))
                    : schemas.ToNewResult<SchemaDiscoveryResult>();
            }

            var paths = new List<DiscoveredPath>();
            var totalContainers = 0;
            var totalFields = 0;

            foreach (var schemaName in schemas.Value)
            {
                var pathResult = await DiscoverPathInternal(connection, schemaName, options, logger, cancellationToken).ConfigureAwait(false);
                if (!pathResult.IsSuccess || pathResult.Value == null)
                {
                    continue;
                }

                paths.Add(pathResult.Value);
                totalContainers += pathResult.Value.Containers.Count;
                foreach (var container in pathResult.Value.Containers)
                {
                    totalFields += container.Fields.Count;
                }
            }

            var result = new SchemaDiscoveryResult
            {
                DatabaseName = databaseName,
                Paths = paths,
                TotalContainers = totalContainers,
                TotalFields = totalFields
            };

            MsSqlSchemaCommandsLog.SchemaDiscoveryCompleted(logger, paths.Count, totalContainers, totalFields);
            return GenericResult<SchemaDiscoveryResult>.Success(result);
        }
        catch (Exception ex)
        {
            return GenericResult<SchemaDiscoveryResult>.Failure(
                MsSqlSchemaCommandsLog.SchemaDiscoveryException(logger, ex));
        }
    }

    /// <summary>
    /// Discovers the schema of the entire database using an MsSqlConnection.
    /// </summary>
    /// <param name="connection">An MsSqlConnection (will be connected if not already).</param>
    /// <param name="options">Discovery options (filters, etc.).</param>
    /// <param name="logger">Optional logger. Uses NullLogger if not provided.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The discovered schema result.</returns>
#pragma warning disable MA0051 // Method is too long - sequential schema discovery and aggregation, not complex
    public static async Task<IGenericResult<SchemaDiscoveryResult>> DiscoverSchema(
        MsSqlConnection connection,
        SchemaDiscoveryOptions? options = null,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
#pragma warning restore MA0051
    {
        logger ??= NullLogger.Instance;
        options ??= SchemaDiscoveryOptions.Default;

        try
        {
            // Ensure connection is open
            var connectResult = await connection.Connect(cancellationToken).ConfigureAwait(false);
            if (!connectResult.IsSuccess)
            {
                return connectResult.ToNewResult<SchemaDiscoveryResult>();
            }

            var databaseName = connection.Database;
            MsSqlSchemaCommandsLog.SchemaDiscoveryStarted(logger, databaseName);

            // Discover schemas using MsSqlConnection's execute capability
            var schemas = await DiscoverSchemasViaMsSql(connection, options, logger, cancellationToken).ConfigureAwait(false);
            if (!schemas.IsSuccess || schemas.Value == null)
            {
                return schemas.IsSuccess
                    ? GenericResult<SchemaDiscoveryResult>.Failure(
                        MsSqlSchemaCommandsLog.SchemaDiscoveryFailed(logger, "Failed to discover schemas"))
                    : schemas.ToNewResult<SchemaDiscoveryResult>();
            }

            var paths = new List<DiscoveredPath>();
            var totalContainers = 0;
            var totalFields = 0;

            foreach (var schemaName in schemas.Value)
            {
                var pathResult = await DiscoverPathViaMsSql(connection, schemaName, options, logger, cancellationToken).ConfigureAwait(false);
                if (!pathResult.IsSuccess || pathResult.Value == null)
                {
                    continue;
                }

                paths.Add(pathResult.Value);
                totalContainers += pathResult.Value.Containers.Count;
                foreach (var container in pathResult.Value.Containers)
                {
                    totalFields += container.Fields.Count;
                }
            }

            var result = new SchemaDiscoveryResult
            {
                DatabaseName = databaseName,
                Paths = paths,
                TotalContainers = totalContainers,
                TotalFields = totalFields
            };

            MsSqlSchemaCommandsLog.SchemaDiscoveryCompleted(logger, paths.Count, totalContainers, totalFields);
            return GenericResult<SchemaDiscoveryResult>.Success(result);
        }
        catch (Exception ex)
        {
            return GenericResult<SchemaDiscoveryResult>.Failure(
                MsSqlSchemaCommandsLog.SchemaDiscoveryException(logger, ex));
        }
    }

    #endregion

    #region Discover Path (Single Schema)

    /// <summary>
    /// Discovers all containers within a single schema using a raw SqlConnection.
    /// </summary>
    /// <param name="connection">An open SqlConnection.</param>
    /// <param name="schemaName">The schema name (e.g., "dbo", "conn", "sec").</param>
    /// <param name="options">Discovery options.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The discovered path with its containers.</returns>
    public static async Task<IGenericResult<DiscoveredPath>> DiscoverPath(
        SqlConnection connection,
        string schemaName,
        SchemaDiscoveryOptions? options = null,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        logger ??= NullLogger.Instance;
        options ??= SchemaDiscoveryOptions.Default;

        if (connection.State != ConnectionState.Open)
        {
            return GenericResult<DiscoveredPath>.Failure(
                MsSqlSchemaCommandsLog.ConnectionNotOpen(logger));
        }

        return await DiscoverPathInternal(connection, schemaName, options, logger, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Discovers all containers within a single schema using an MsSqlConnection.
    /// </summary>
    /// <param name="connection">An MsSqlConnection.</param>
    /// <param name="schemaName">The schema name (e.g., "dbo", "conn", "sec").</param>
    /// <param name="options">Discovery options.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The discovered path with its containers.</returns>
    public static async Task<IGenericResult<DiscoveredPath>> DiscoverPath(
        MsSqlConnection connection,
        string schemaName,
        SchemaDiscoveryOptions? options = null,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        logger ??= NullLogger.Instance;
        options ??= SchemaDiscoveryOptions.Default;

        var connectResult = await connection.Connect(cancellationToken).ConfigureAwait(false);
        if (!connectResult.IsSuccess)
        {
            return connectResult.ToNewResult<DiscoveredPath>();
        }

        return await DiscoverPathViaMsSql(connection, schemaName, options, logger, cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Discover Container (Single Table/View)

    /// <summary>
    /// Discovers a single table or view using a raw SqlConnection.
    /// </summary>
    /// <param name="connection">An open SqlConnection.</param>
    /// <param name="schemaName">The schema name.</param>
    /// <param name="tableName">The table or view name.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The discovered container.</returns>
    public static async Task<IGenericResult<DiscoveredContainer>> DiscoverContainer(
        SqlConnection connection,
        string schemaName,
        string tableName,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        logger ??= NullLogger.Instance;

        if (connection.State != ConnectionState.Open)
        {
            return GenericResult<DiscoveredContainer>.Failure(
                MsSqlSchemaCommandsLog.ConnectionNotOpen(logger));
        }

        return await DiscoverContainerInternal(connection, schemaName, tableName, logger, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Discovers a single table or view using an MsSqlConnection.
    /// </summary>
    /// <param name="connection">An MsSqlConnection.</param>
    /// <param name="schemaName">The schema name.</param>
    /// <param name="tableName">The table or view name.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The discovered container.</returns>
    public static async Task<IGenericResult<DiscoveredContainer>> DiscoverContainer(
        MsSqlConnection connection,
        string schemaName,
        string tableName,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        logger ??= NullLogger.Instance;

        var connectResult = await connection.Connect(cancellationToken).ConfigureAwait(false);
        if (!connectResult.IsSuccess)
        {
            return connectResult.ToNewResult<DiscoveredContainer>();
        }

        return await DiscoverContainerViaMsSql(connection, schemaName, tableName, logger, cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Persist Schema

    /// <summary>
    /// Persists discovered schema to configuration tables.
    /// </summary>
    /// <param name="schema">The discovered schema to persist.</param>
    /// <param name="connectionId">The connection ID to associate the DataStore with.</param>
    /// <param name="connectionName">The connection name for the DataStore name.</param>
    /// <param name="dataStoreProvider">DataStore configuration provider for looking up existing configurations.</param>
    /// <param name="dataPathProvider">DataPath configuration provider for save operations.</param>
    /// <param name="containerProvider">DataContainer configuration provider for save operations.</param>
    /// <param name="fieldProvider">DataContainerField configuration provider for save operations.</param>
    /// <param name="dataPathOptions">Existing data path configurations.</param>
    /// <param name="dataContainerOptions">Existing data container configurations.</param>
    /// <param name="dataContainerFieldOptions">Existing data container field configurations.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The persistence result with statistics.</returns>
    [ConventionOverride(MaxCyclomaticComplexity = 30, MaxMethodLines = 145)]
    public static async Task<IGenericResult<SchemaPersistResult>> PersistSchema(
        SchemaDiscoveryResult schema,
        Guid connectionId,
        string connectionName,
        DataStoreConfigurationProvider dataStoreProvider,
        DefaultConfigurationProvider<DataPathConfiguration, DataPathConfigurationCommand> dataPathProvider,
        DefaultConfigurationProvider<DataContainerConfiguration, DataContainerConfigurationCommand> containerProvider,
        DefaultConfigurationProvider<DataContainerFieldConfiguration, DataContainerFieldConfigurationCommand> fieldProvider,
        IOptionsMonitor<List<DataPathConfiguration>> dataPathOptions,
        IOptionsMonitor<List<DataContainerConfiguration>> dataContainerOptions,
        IOptionsMonitor<List<DataContainerFieldConfiguration>> dataContainerFieldOptions,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        logger ??= NullLogger.Instance;

        try
        {
            MsSqlSchemaCommandsLog.SchemaPersistStarted(logger, connectionName, schema.TotalContainers);

            // Check if DataStore already exists for this connection
            var allDataStoresResult = await dataStoreProvider.Get(cancellationToken).ConfigureAwait(false);
            var allDataStores = allDataStoresResult.IsSuccess ? allDataStoresResult.Value! : (IReadOnlyList<DataStoreConfiguration>)[];
            var existingDataStore = allDataStores.FirstOrDefault(ds => ds.ConnectionId == connectionId);

            bool isNewDataStore = existingDataStore == null;
            Guid dataStoreId;
            int pathsAdded = 0, pathsModified = 0;
            int containersAdded = 0, containersModified = 0;
            int fieldsAdded = 0, fieldsModified = 0;

            // Create or get DataStore
            if (isNewDataStore)
            {
                var dataStore = new DataStoreConfiguration
                {
                    Id = Guid.NewGuid(),
                    Name = $"{connectionName}_DataStore",
                    ConnectionId = connectionId,
                    ServiceOptionType = "MsSql",
                    Description = $"Auto-discovered from connection '{connectionName}' to database '{schema.DatabaseName}'"
                };

                var saveResult = await dataStoreProvider.Save(dataStore, cancellationToken).ConfigureAwait(false);
                if (!saveResult.IsSuccess || saveResult.Value == null)
                {
                    return GenericResult<SchemaPersistResult>.Failure(
                        MsSqlSchemaCommandsLog.SaveFailed(logger, "DataStore", saveResult.CurrentMessage ?? "Unknown error"));
                }

                dataStoreId = saveResult.Value.Id;
            }
            else
            {
                dataStoreId = existingDataStore!.Id;
            }

            var pathWriter = dataPathProvider;
            var containerWriter = containerProvider;
            var fieldWriter = fieldProvider;

            // Process paths
            foreach (var discoveredPath in schema.Paths)
            {
                var existingPath = dataPathOptions.CurrentValue
                    .FirstOrDefault(p => p.DataStoreId == dataStoreId && string.Equals(p.Name, discoveredPath.SchemaName, StringComparison.Ordinal));

                DataPathConfiguration path;
                if (existingPath == null)
                {
                    path = new DataPathConfiguration
                    {
                        Id = Guid.NewGuid(),
                        Name = discoveredPath.SchemaName,
                        DataStoreId = dataStoreId,
                        PathValue = discoveredPath.SchemaName,
                        PathType = "Schema"
                    };

                    var pathSaveResult = await pathWriter.Save(path, cancellationToken).ConfigureAwait(false);
                    if (!pathSaveResult.IsSuccess || pathSaveResult.Value == null)
                    {
                        return GenericResult<SchemaPersistResult>.Failure(
                            MsSqlSchemaCommandsLog.SaveFailed(logger, "DataPath", pathSaveResult.CurrentMessage ?? "Unknown error"));
                    }

                    path = pathSaveResult.Value;
                    pathsAdded++;
                }
                else
                {
                    path = existingPath;
                    pathsModified++;
                }

                // Process containers
                foreach (var discoveredContainer in discoveredPath.Containers)
                {
                    var existingContainer = dataContainerOptions.CurrentValue
                        .FirstOrDefault(c => c.DataPathId == path.Id && string.Equals(c.Name, discoveredContainer.Name, StringComparison.Ordinal));

                    DataContainerConfiguration container;
                    if (existingContainer == null)
                    {
                        var containerId = Guid.NewGuid();

                        container = new DataContainerConfiguration
                        {
                            Id = containerId,
                            Name = discoveredContainer.Name,
                            DataPathId = path.Id,
                            // Why: TypeId replaces ContainerType after Wave A5 DDL rename.
                            TypeId = discoveredContainer.ContainerType,
                            Description = discoveredContainer.Description
                            // Why: SurrogateKeyFields removed in Wave A5 — keys are now parent-child
                            // DataContainerKey + DataContainerKeyField rows. Key synthesis deferred to Wave B2.
                        };

                        var containerSaveResult = await containerWriter.Save(container, cancellationToken).ConfigureAwait(false);
                        if (!containerSaveResult.IsSuccess || containerSaveResult.Value == null)
                        {
                            return GenericResult<SchemaPersistResult>.Failure(
                                MsSqlSchemaCommandsLog.SaveFailed(logger, "DataContainer", containerSaveResult.CurrentMessage ?? "Unknown error"));
                        }

                        container = containerSaveResult.Value;
                        containersAdded++;
                    }
                    else
                    {
                        container = existingContainer;
                        containersModified++;
                    }

                    // Process fields
                    foreach (var discoveredField in discoveredContainer.Fields)
                    {
                        var existingField = dataContainerFieldOptions.CurrentValue
                            .FirstOrDefault(f => f.DataContainerId == container.Id && string.Equals(f.Name, discoveredField.Name, StringComparison.Ordinal));

                        if (existingField == null)
                        {
                            var field = new DataContainerFieldConfiguration
                            {
                                Id = Guid.NewGuid(),
                                Name = discoveredField.Name,
                                DataContainerId = container.Id,
                                DataType = discoveredField.SqlType,
                                Description = discoveredField.Description
                                // Why: IsNullable, Ordinal, MaxLength, Precision, Scale, DefaultValue
                                // moved to data.MsSqlDataContainerField typed body (Wave A5). Wave B2 work.
                            };

                            var fieldSaveResult = await fieldWriter.Save(field, cancellationToken).ConfigureAwait(false);
                            if (!fieldSaveResult.IsSuccess)
                            {
                                return GenericResult<SchemaPersistResult>.Failure(
                                    MsSqlSchemaCommandsLog.SaveFailed(logger, "DataContainerField", fieldSaveResult.CurrentMessage ?? "Unknown error"));
                            }

                            fieldsAdded++;
                        }
                        else
                        {
                            fieldsModified++;
                        }
                    }
                }
            }

            var result = new SchemaPersistResult
            {
                DataStoreId = dataStoreId,
                IsNewDataStore = isNewDataStore,
                PathsAdded = pathsAdded,
                PathsModified = pathsModified,
                ContainersAdded = containersAdded,
                ContainersModified = containersModified,
                FieldsAdded = fieldsAdded,
                FieldsModified = fieldsModified
            };

            MsSqlSchemaCommandsLog.SchemaPersistCompleted(logger, dataStoreId, pathsAdded, containersAdded, fieldsAdded);
            return GenericResult<SchemaPersistResult>.Success(result);
        }
        catch (Exception ex)
        {
            return GenericResult<SchemaPersistResult>.Failure(
                MsSqlSchemaCommandsLog.SchemaPersistException(logger, ex));
        }
    }

    #endregion

    #region Internal Methods - SqlConnection

    private static async Task<IGenericResult<List<string>>> DiscoverSchemasInternal(
        SqlConnection connection,
        SchemaDiscoveryOptions options,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        const string query = @"
            SELECT SCHEMA_NAME
            FROM INFORMATION_SCHEMA.SCHEMATA
            WHERE SCHEMA_NAME NOT IN ('sys', 'INFORMATION_SCHEMA', 'guest')
            ORDER BY SCHEMA_NAME";

        var schemas = new List<string>();

        using var command = new SqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var schemaName = reader.GetString(0);
            if (MatchesFilter(schemaName, options.IncludeOnlySchemas, options.ExcludedSchemas))
            {
                schemas.Add(schemaName);
            }
        }

        return GenericResult<List<string>>.Success(schemas);
    }

    private static async Task<IGenericResult<DiscoveredPath>> DiscoverPathInternal(
        SqlConnection connection,
        string schemaName,
        SchemaDiscoveryOptions options,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var containers = new List<DiscoveredContainer>();

        // Discover tables (always included unless we filter by pattern)
        var tablesResult = await DiscoverContainersInSchema(connection, schemaName, "BASE TABLE", options, logger, cancellationToken).ConfigureAwait(false);
        if (!tablesResult.IsSuccess || tablesResult.Value == null)
        {
            return tablesResult.ToNewResult<DiscoveredPath>();
        }
        containers.AddRange(tablesResult.Value);

        // Discover views
        if (options.DiscoverViews)
        {
            var viewsResult = await DiscoverContainersInSchema(connection, schemaName, "VIEW", options, logger, cancellationToken).ConfigureAwait(false);
            if (!viewsResult.IsSuccess || viewsResult.Value == null)
            {
                return viewsResult.ToNewResult<DiscoveredPath>();
            }
            containers.AddRange(viewsResult.Value);
        }

        // Set schema name on containers
        foreach (var container in containers)
        {
            container.SchemaName = schemaName;
        }

        var path = new DiscoveredPath
        {
            SchemaName = schemaName,
            Containers = containers
        };

        return GenericResult<DiscoveredPath>.Success(path);
    }

    private static async Task<IGenericResult<List<DiscoveredContainer>>> DiscoverContainersInSchema(
        SqlConnection connection,
        string schemaName,
        string tableType,
        SchemaDiscoveryOptions options,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        const string query = @"
            SELECT TABLE_NAME
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = @SchemaName AND TABLE_TYPE = @TableType
            ORDER BY TABLE_NAME";

        var containers = new List<DiscoveredContainer>();

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@SchemaName", schemaName);
        command.Parameters.AddWithValue("@TableType", tableType);

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        var tableNames = new List<string>();

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var tableName = reader.GetString(0);
            if (MatchesTableFilter(tableName, options.ExcludedTablePatterns))
            {
                tableNames.Add(tableName);
            }
        }

        await reader.CloseAsync().ConfigureAwait(false);

        foreach (var tableName in tableNames)
        {
            var containerResult = await DiscoverContainerInternal(connection, schemaName, tableName, logger, cancellationToken).ConfigureAwait(false);
            if (!containerResult.IsSuccess || containerResult.Value == null)
            {
                continue;
            }
            containers.Add(containerResult.Value);
        }

        return GenericResult<List<DiscoveredContainer>>.Success(containers);
    }

    [ConventionOverride(MaxMethodLines = 90)]
    private static async Task<IGenericResult<DiscoveredContainer>> DiscoverContainerInternal(
        SqlConnection connection,
        string schemaName,
        string tableName,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        // Get columns with extended properties
        const string columnsQuery = @"
            SELECT
                c.COLUMN_NAME,
                c.DATA_TYPE,
                c.IS_NULLABLE,
                c.ORDINAL_POSITION,
                c.CHARACTER_MAXIMUM_LENGTH,
                c.NUMERIC_PRECISION,
                c.NUMERIC_SCALE,
                c.COLUMN_DEFAULT,
                CAST(ep.value AS NVARCHAR(4000)) AS Description
            FROM INFORMATION_SCHEMA.COLUMNS c
            LEFT JOIN sys.extended_properties ep
                ON ep.major_id = OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME)
                AND ep.minor_id = c.ORDINAL_POSITION
                AND ep.name = 'MS_Description'
            WHERE c.TABLE_SCHEMA = @SchemaName AND c.TABLE_NAME = @TableName
            ORDER BY c.ORDINAL_POSITION";

        // Get primary key columns
        const string pkQuery = @"
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
            JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                ON kcu.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
                AND kcu.TABLE_SCHEMA = tc.TABLE_SCHEMA
            WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                AND kcu.TABLE_SCHEMA = @SchemaName
                AND kcu.TABLE_NAME = @TableName";

        // Get table description
        const string tableDescQuery = @"
            SELECT CAST(ep.value AS NVARCHAR(4000))
            FROM sys.extended_properties ep
            JOIN sys.tables t ON ep.major_id = t.object_id
            JOIN sys.schemas s ON t.schema_id = s.schema_id
            WHERE ep.minor_id = 0
                AND ep.name = 'MS_Description'
                AND s.name = @SchemaName
                AND t.name = @TableName";

        // Get object type
        const string typeQuery = @"
            SELECT TABLE_TYPE FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = @SchemaName AND TABLE_NAME = @TableName";

        var fields = new List<DiscoveredField>();
        var primaryKeyColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? tableDescription = null;
        string containerType = "Table";

        // Get type
        using (var typeCmd = new SqlCommand(typeQuery, connection))
        {
            typeCmd.Parameters.AddWithValue("@SchemaName", schemaName);
            typeCmd.Parameters.AddWithValue("@TableName", tableName);
            var typeResult = await typeCmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            containerType = string.Equals(typeResult?.ToString(), "VIEW", StringComparison.Ordinal) ? "View" : "Table";
        }

        // Get primary keys
        using (var pkCmd = new SqlCommand(pkQuery, connection))
        {
            pkCmd.Parameters.AddWithValue("@SchemaName", schemaName);
            pkCmd.Parameters.AddWithValue("@TableName", tableName);
            using var pkReader = await pkCmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await pkReader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                primaryKeyColumns.Add(pkReader.GetString(0));
            }
        }

        // Get table description
        using (var descCmd = new SqlCommand(tableDescQuery, connection))
        {
            descCmd.Parameters.AddWithValue("@SchemaName", schemaName);
            descCmd.Parameters.AddWithValue("@TableName", tableName);
            var descResult = await descCmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            tableDescription = descResult as string;
        }

        // Get columns
        using (var colCmd = new SqlCommand(columnsQuery, connection))
        {
            colCmd.Parameters.AddWithValue("@SchemaName", schemaName);
            colCmd.Parameters.AddWithValue("@TableName", tableName);
            using var colReader = await colCmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            while (await colReader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var columnName = colReader.GetString(0);
                var field = new DiscoveredField
                {
                    Name = columnName,
                    SqlType = colReader.GetString(1),
                    IsNullable = string.Equals(colReader.GetString(2), "YES", StringComparison.Ordinal),
                    Ordinal = colReader.GetInt32(3),
                    MaxLength = await colReader.IsDBNullAsync(4, cancellationToken).ConfigureAwait(false) ? null : colReader.GetInt32(4),
                    Precision = await colReader.IsDBNullAsync(5, cancellationToken).ConfigureAwait(false) ? null : (int)colReader.GetByte(5),
                    Scale = await colReader.IsDBNullAsync(6, cancellationToken).ConfigureAwait(false) ? null : colReader.GetInt32(6),
                    DefaultValue = await colReader.IsDBNullAsync(7, cancellationToken).ConfigureAwait(false) ? null : colReader.GetString(7),
                    Description = await colReader.IsDBNullAsync(8, cancellationToken).ConfigureAwait(false) ? null : colReader.GetString(8),
                };
                fields.Add(field);
            }
        }

        var container = new DiscoveredContainer
        {
            Name = tableName,
            ContainerType = containerType,
            Fields = fields,
            PrimaryKeyColumns = primaryKeyColumns.ToList(),
            Indexes = [],
            Description = tableDescription
        };

        return GenericResult<DiscoveredContainer>.Success(container);
    }

    #endregion

    #region Internal Methods - MsSqlConnection

    private static async Task<IGenericResult<List<string>>> DiscoverSchemasViaMsSql(
        MsSqlConnection connection,
        SchemaDiscoveryOptions options,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        // For now, delegate to internal implementation
        // MsSqlConnection exposes ConnectionString but not SqlConnection directly
        // We can create a temporary SqlConnection for schema discovery
        using var sqlConnection = connection.CreatePooledConnection();
        await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await DiscoverSchemasInternal(sqlConnection, options, logger, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IGenericResult<DiscoveredPath>> DiscoverPathViaMsSql(
        MsSqlConnection connection,
        string schemaName,
        SchemaDiscoveryOptions options,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        using var sqlConnection = connection.CreatePooledConnection();
        await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await DiscoverPathInternal(sqlConnection, schemaName, options, logger, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IGenericResult<DiscoveredContainer>> DiscoverContainerViaMsSql(
        MsSqlConnection connection,
        string schemaName,
        string tableName,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        using var sqlConnection = connection.CreatePooledConnection();
        await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await DiscoverContainerInternal(sqlConnection, schemaName, tableName, logger, cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Helpers

    private static bool MatchesFilter(string value, IReadOnlyList<string>? includes, IReadOnlyList<string>? excludes)
    {
        if (excludes != null && excludes.Count > 0)
        {
            if (excludes.Any(e => string.Equals(e, value, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
        }

        if (includes != null && includes.Count > 0)
        {
            return includes.Any(i => string.Equals(i, value, StringComparison.OrdinalIgnoreCase));
        }

        return true;
    }

    private static bool MatchesTableFilter(string tableName, IReadOnlyList<string>? excludePatterns)
    {
        if (excludePatterns == null || excludePatterns.Count == 0)
        {
            return true;
        }

        // Simple pattern matching: check if table name starts with any exclude pattern
        // SQL LIKE '%pattern%' style matching
        foreach (var pattern in excludePatterns)
        {
            // Remove LIKE wildcards for simple matching
            var cleanPattern = pattern.Replace("%", string.Empty);
            if (tableName.StartsWith(cleanPattern, StringComparison.OrdinalIgnoreCase) ||
                tableName.EndsWith(cleanPattern, StringComparison.OrdinalIgnoreCase) ||
                tableName.Contains(cleanPattern, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    #endregion
}
