using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceDataSets.Endpoints;

/// <summary>The coded messages the fdwreferencedata endpoints answer a refusal or a fault with: one per reason, each with the client text it shows (FDW-2335).</summary>
[MessageLoggingTypeCode("RSTDATAANSWERS")]
public static partial class FdwReferenceDataAnswerLog
{

    /// <summary>Logs that The tree read answered a success and no value.</summary>
    [MessageLogging(EventId = 90001, Level = LogLevel.Error, Message = "The tree read answered a success and no value", ClientMessage = "The tree could not be read")]
    public static partial IGenericMessage SearchDataSetSourceTreeReturnedNothing(ILogger logger);

    /// <summary>Logs that The label read answered a success and no value.</summary>
    [MessageLogging(EventId = 90002, Level = LogLevel.Error, Message = "The label read answered a success and no value", ClientMessage = "The label could not be read")]
    public static partial IGenericMessage SearchDataSetSourceLabelReturnedNothing(ILogger logger);

    /// <summary>Logs that Map data set source was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30001, Level = LogLevel.Warning, Message = "Map data set source was not found, or is not visible to the caller", ClientMessage = "Map data set source was not found")]
    public static partial IGenericMessage MapDataSetSourceMapdatasetsourceNotFound(ILogger logger);

    /// <summary>Logs that Map data set source was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30002, Level = LogLevel.Warning, Message = "Map data set source was not found, or is not visible to the caller", ClientMessage = "Map data set source was not found")]
    public static partial IGenericMessage MapDataSetSourceNotFound(ILogger logger);

    /// <summary>Logs that Map data set source was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30003, Level = LogLevel.Warning, Message = "Map data set source was not found, or is not visible to the caller", ClientMessage = "Map data set source was not found")]
    public static partial IGenericMessage MapDataSetSourceNotFound2(ILogger logger);

    /// <summary>Logs that Map data set source was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30004, Level = LogLevel.Warning, Message = "Map data set source was not found, or is not visible to the caller", ClientMessage = "Map data set source was not found")]
    public static partial IGenericMessage MapDataSetSourceNotFound3(ILogger logger);

    /// <summary>Logs that Map data set source was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30005, Level = LogLevel.Warning, Message = "Map data set source was not found, or is not visible to the caller", ClientMessage = "Map data set source was not found")]
    public static partial IGenericMessage MapDataSetSourceNotFound4(ILogger logger);
}
