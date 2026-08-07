using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Reference.Management.UI.Tailwind.Logging;

/// <summary>
/// Structured logging for Calculation operations.
/// EventId range: 1770-1779
/// </summary>
public static partial class CalculationLog
{
    /// <summary>Logs that calculation definitions were loaded.</summary>
    [MessageLogging(EventId = 1770, Level = LogLevel.Information, Message = "Calculation definitions loaded ({count} items)")]
    public static partial IGenericMessage DefinitionsLoaded(ILogger logger, int count);

    /// <summary>Logs that a specific calculation definition was loaded.</summary>
    [MessageLogging(EventId = 1771, Level = LogLevel.Information, Message = "Calculation definition '{id}' loaded")]
    public static partial IGenericMessage DefinitionLoaded(ILogger logger, Guid id);

    /// <summary>Logs that a calculation definition was created.</summary>
    [MessageLogging(EventId = 1772, Level = LogLevel.Information, Message = "Calculation definition created")]
    public static partial IGenericMessage DefinitionCreated(ILogger logger);

    /// <summary>Logs that a calculation definition was updated.</summary>
    [MessageLogging(EventId = 1773, Level = LogLevel.Information, Message = "Calculation definition '{id}' updated")]
    public static partial IGenericMessage DefinitionUpdated(ILogger logger, Guid id);

    /// <summary>Logs that formula validation was requested.</summary>
    [MessageLogging(EventId = 1774, Level = LogLevel.Information, Message = "Formula validation requested")]
    public static partial IGenericMessage FormulaValidationRequested(ILogger logger);

    /// <summary>Logs that formula preview was requested.</summary>
    [MessageLogging(EventId = 1775, Level = LogLevel.Information, Message = "Formula preview requested")]
    public static partial IGenericMessage FormulaPreviewRequested(ILogger logger);

    /// <summary>Logs a calculation operation failure.</summary>
    [MessageLogging(EventId = 1776, Level = LogLevel.Error, Message = "Calculation operation failed")]
    public static partial IGenericMessage CalculationOperationFailed(ILogger logger, Exception ex);

    /// <summary>Logs that DataSet fields were loaded.</summary>
    [MessageLogging(EventId = 1777, Level = LogLevel.Information, Message = "DataSet fields loaded for '{dataSetName}' ({count} fields)")]
    public static partial IGenericMessage DataSetFieldsLoaded(ILogger logger, string dataSetName, int count);

    /// <summary>Logs that formula validation failed.</summary>
    [MessageLogging(EventId = 1778, Level = LogLevel.Warning, Message = "Formula validation failed: {reason}")]
    public static partial IGenericMessage FormulaValidationFailed(ILogger logger, string reason);

    /// <summary>Logs that calculation DataSets were loaded.</summary>
    [MessageLogging(EventId = 1779, Level = LogLevel.Information, Message = "Calculation DataSets loaded ({count} items)")]
    public static partial IGenericMessage DataSetsLoaded(ILogger logger, int count);
}
