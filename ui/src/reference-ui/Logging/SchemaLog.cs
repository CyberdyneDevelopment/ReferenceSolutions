using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Reference.Ui.Logging;

/// <summary>
/// Structured logging for Schema explorer operations.
/// EventId range: 1790-1799
/// </summary>
public static partial class SchemaLog
{
    /// <summary>Logs that schema was loaded for a connection.</summary>
    [MessageLogging(EventId = 1790, Level = LogLevel.Information, Message = "Schema loaded for connection '{connectionName}' ({schemaCount} schemas)")]
    public static partial IGenericMessage SchemaLoaded(ILogger logger, string connectionName, int schemaCount);

    /// <summary>Logs that schema discovery started.</summary>
    [MessageLogging(EventId = 1791, Level = LogLevel.Information, Message = "Schema discovery started for connection '{connectionName}'")]
    public static partial IGenericMessage DiscoveryStarted(ILogger logger, string connectionName);

    /// <summary>Logs that schema discovery completed.</summary>
    [MessageLogging(EventId = 1792, Level = LogLevel.Information, Message = "Schema discovery completed for '{connectionName}'")]
    public static partial IGenericMessage DiscoveryCompleted(ILogger logger, string connectionName);

    /// <summary>Logs a schema discovery failure.</summary>
    [MessageLogging(EventId = 1793, Level = LogLevel.Error, Message = "Schema discovery failed for '{connectionName}'")]
    public static partial IGenericMessage DiscoveryFailed(ILogger logger, Exception ex, string connectionName);

    /// <summary>Logs that a data preview was executed.</summary>
    [MessageLogging(EventId = 1794, Level = LogLevel.Information, Message = "Data preview executed for '{connectionName}' table '{tableName}' ({rowCount} rows)")]
    public static partial IGenericMessage DataPreviewExecuted(ILogger logger, string connectionName, string tableName, int rowCount);

    /// <summary>Logs a data preview failure.</summary>
    [MessageLogging(EventId = 1795, Level = LogLevel.Error, Message = "Data preview failed for '{connectionName}' table '{tableName}'")]
    public static partial IGenericMessage DataPreviewFailed(ILogger logger, Exception ex, string connectionName, string tableName);

    /// <summary>Logs that schema-capable connections were loaded.</summary>
    [MessageLogging(EventId = 1796, Level = LogLevel.Information, Message = "Schema-capable connections loaded ({count} connections)")]
    public static partial IGenericMessage SchemaCapableConnectionsLoaded(ILogger logger, int count);

    /// <summary>Logs that a table was selected in the schema explorer.</summary>
    [MessageLogging(EventId = 1797, Level = LogLevel.Information, Message = "Table '{tableName}' selected in schema '{schemaName}'")]
    public static partial IGenericMessage TableSelected(ILogger logger, string tableName, string schemaName);

    /// <summary>Logs a failure to load schema for a connection.</summary>
    [MessageLogging(EventId = 1798, Level = LogLevel.Error, Message = "Failed to load schema for connection '{connectionName}'")]
    public static partial IGenericMessage SchemaLoadFailed(ILogger logger, Exception ex, string connectionName);

    /// <summary>Logs that a schema filter was applied.</summary>
    [MessageLogging(EventId = 1799, Level = LogLevel.Information, Message = "Schema filter applied: '{schemaName}' ({tableCount} tables, {viewCount} views)")]
    public static partial IGenericMessage SchemaFilterApplied(ILogger logger, string schemaName, int tableCount, int viewCount);
}
