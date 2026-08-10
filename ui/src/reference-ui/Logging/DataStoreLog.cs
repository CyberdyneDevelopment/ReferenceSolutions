using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Reference.Ui.Logging;

/// <summary>
/// Structured logging for DataStore operations.
/// EventId range: 1760-1769
/// </summary>
public static partial class DataStoreLog
{
    /// <summary>Logs that the DataStore list was loaded.</summary>
    [MessageLogging(EventId = 1760, Level = LogLevel.Information, Message = "DataStore list loaded ({count} items)")]
    public static partial IGenericMessage DataStoreListLoaded(ILogger logger, int count);

    /// <summary>Logs that a specific DataStore was loaded.</summary>
    [MessageLogging(EventId = 1761, Level = LogLevel.Information, Message = "DataStore '{name}' loaded")]
    public static partial IGenericMessage DataStoreLoaded(ILogger logger, string name);

    /// <summary>Logs that a DataStore was created.</summary>
    [MessageLogging(EventId = 1762, Level = LogLevel.Information, Message = "DataStore '{name}' created")]
    public static partial IGenericMessage DataStoreCreated(ILogger logger, string name);

    /// <summary>Logs that a DataStore was updated.</summary>
    [MessageLogging(EventId = 1763, Level = LogLevel.Information, Message = "DataStore '{name}' updated")]
    public static partial IGenericMessage DataStoreUpdated(ILogger logger, string name);

    /// <summary>Logs that a DataStore was deleted.</summary>
    [MessageLogging(EventId = 1764, Level = LogLevel.Information, Message = "DataStore '{name}' deleted")]
    public static partial IGenericMessage DataStoreDeleted(ILogger logger, string name);

    /// <summary>Logs a DataStore operation failure.</summary>
    [MessageLogging(EventId = 1765, Level = LogLevel.Error, Message = "DataStore operation failed for '{name}'")]
    public static partial IGenericMessage DataStoreOperationFailed(ILogger logger, Exception ex, string name);

    /// <summary>Logs that container discovery started.</summary>
    [MessageLogging(EventId = 1766, Level = LogLevel.Information, Message = "Container discovery started for DataStore '{name}'")]
    public static partial IGenericMessage ContainerDiscoveryStarted(ILogger logger, string name);

    /// <summary>Logs that container discovery completed.</summary>
    [MessageLogging(EventId = 1767, Level = LogLevel.Information, Message = "Container discovery completed for DataStore '{name}' ({count} containers)")]
    public static partial IGenericMessage ContainerDiscoveryCompleted(ILogger logger, string name, int count);

    /// <summary>Logs a container discovery failure.</summary>
    [MessageLogging(EventId = 1768, Level = LogLevel.Error, Message = "Container discovery failed for DataStore '{name}'")]
    public static partial IGenericMessage ContainerDiscoveryFailed(ILogger logger, Exception ex, string name);

    /// <summary>Logs that DataStore paths were loaded.</summary>
    [MessageLogging(EventId = 1769, Level = LogLevel.Information, Message = "DataStore paths loaded for '{name}' ({count} paths)")]
    public static partial IGenericMessage DataStorePathsLoaded(ILogger logger, string name, int count);
}
