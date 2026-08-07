using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Reference.Management.UI.Tailwind.Logging;

/// <summary>
/// Structured logging for Configuration browser operations.
/// EventId range: 1780-1789
/// </summary>
public static partial class ConfigurationLog
{
    /// <summary>Logs that configuration types were loaded.</summary>
    [MessageLogging(EventId = 1780, Level = LogLevel.Information, Message = "Configuration types loaded ({count} root types)")]
    public static partial IGenericMessage TypesLoaded(ILogger logger, int count);

    /// <summary>Logs that a configuration type was selected.</summary>
    [MessageLogging(EventId = 1781, Level = LogLevel.Information, Message = "Configuration type '{typeName}' selected")]
    public static partial IGenericMessage TypeSelected(ILogger logger, string typeName);

    /// <summary>Logs that configuration instances were loaded for a type.</summary>
    [MessageLogging(EventId = 1782, Level = LogLevel.Information, Message = "Configuration instances loaded for '{typeName}' ({count} instances)")]
    public static partial IGenericMessage InstancesLoaded(ILogger logger, string typeName, int count);

    /// <summary>Logs that a specific configuration instance was loaded.</summary>
    [MessageLogging(EventId = 1783, Level = LogLevel.Information, Message = "Configuration instance '{instanceId}' loaded")]
    public static partial IGenericMessage InstanceLoaded(ILogger logger, string instanceId);

    /// <summary>Logs that a configuration instance was created.</summary>
    [MessageLogging(EventId = 1784, Level = LogLevel.Information, Message = "Configuration instance created for type '{typeName}'")]
    public static partial IGenericMessage InstanceCreated(ILogger logger, string typeName);

    /// <summary>Logs that a configuration instance was updated.</summary>
    [MessageLogging(EventId = 1785, Level = LogLevel.Information, Message = "Configuration instance '{instanceId}' updated")]
    public static partial IGenericMessage InstanceUpdated(ILogger logger, string instanceId);

    /// <summary>Logs that a configuration instance was deleted.</summary>
    [MessageLogging(EventId = 1786, Level = LogLevel.Information, Message = "Configuration instance '{instanceId}' deleted")]
    public static partial IGenericMessage InstanceDeleted(ILogger logger, string instanceId);

    /// <summary>Logs a configuration operation failure.</summary>
    [MessageLogging(EventId = 1787, Level = LogLevel.Error, Message = "Configuration operation failed for type '{typeName}'")]
    public static partial IGenericMessage ConfigurationOperationFailed(ILogger logger, Exception ex, string typeName);

    /// <summary>Logs that a configuration type has no properties defined.</summary>
    [MessageLogging(EventId = 1788, Level = LogLevel.Warning, Message = "Configuration type '{typeName}' has no properties defined")]
    public static partial IGenericMessage NoPropertiesDefined(ILogger logger, string typeName);

    /// <summary>Logs a failure to load configuration root types.</summary>
    [MessageLogging(EventId = 1789, Level = LogLevel.Error, Message = "Failed to load configuration root types")]
    public static partial IGenericMessage RootTypesLoadFailed(ILogger logger, Exception ex);
}
