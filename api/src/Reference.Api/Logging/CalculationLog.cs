using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Logging;

/// <summary>
/// MessageLogging definitions for Calculation management endpoints.
/// EventId range: 1500-1549
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class CalculationLog
{
    // List operations (1500-1509)
    [MessageLogging(EventId = 1500, Level = LogLevel.Information, Message = "Listing available calculation types")]
    public static partial IGenericMessage ListingCalculationTypes(ILogger logger);

    [MessageLogging(EventId = 1501, Level = LogLevel.Information, Message = "Retrieved {count} calculation types")]
    public static partial IGenericMessage CalculationTypesRetrieved(ILogger logger, int count);

    [MessageLogging(EventId = 1502, Level = LogLevel.Information, Message = "Listing available period comparison types")]
    public static partial IGenericMessage ListingPeriodComparisonTypes(ILogger logger);

    [MessageLogging(EventId = 1503, Level = LogLevel.Information, Message = "Retrieved {count} period comparison types")]
    public static partial IGenericMessage PeriodComparisonTypesRetrieved(ILogger logger, int count);

    // Execute operations (1510-1519)
    [MessageLogging(EventId = 1510, Level = LogLevel.Information, Message = "Executing calculation '{calculationType}' on {rowCount} rows")]
    public static partial IGenericMessage ExecutingCalculation(ILogger logger, string calculationType, int rowCount);

    [MessageLogging(EventId = 1511, Level = LogLevel.Information, Message = "Calculation '{calculationType}' completed with result: {result}")]
    public static partial IGenericMessage CalculationCompleted(ILogger logger, string calculationType, decimal result);

    [MessageLogging(EventId = 1512, Level = LogLevel.Error, Message = "Calculation '{calculationType}' failed: {message}")]
    public static partial IGenericMessage CalculationFailed(ILogger logger, string calculationType, string message);

    [MessageLogging(EventId = 1513, Level = LogLevel.Warning, Message = "Unknown calculation type '{calculationType}'")]
    public static partial IGenericMessage UnknownCalculationType(ILogger logger, string calculationType);

    [MessageLogging(EventId = 1514, Level = LogLevel.Warning, Message = "No values provided for calculation")]
    public static partial IGenericMessage NoValuesProvided(ILogger logger);

    // Preview operations (1520-1529)
    [MessageLogging(EventId = 1520, Level = LogLevel.Information, Message = "Previewing calculation '{calculationType}' on sample data")]
    public static partial IGenericMessage PreviewingCalculation(ILogger logger, string calculationType);

    [MessageLogging(EventId = 1521, Level = LogLevel.Information, Message = "Preview completed for '{calculationType}'")]
    public static partial IGenericMessage PreviewCompleted(ILogger logger, string calculationType);

    [MessageLogging(EventId = 1522, Level = LogLevel.Error, Message = "Preview failed for '{calculationType}': {message}")]
    public static partial IGenericMessage PreviewFailed(ILogger logger, string calculationType, string message);

    // Validation operations (1530-1539)
    [MessageLogging(EventId = 1530, Level = LogLevel.Warning, Message = "Validation failed: {message}")]
    public static partial IGenericMessage ValidationFailed(ILogger logger, string message);

    [MessageLogging(EventId = 1531, Level = LogLevel.Warning, Message = "Invalid period comparison type '{periodType}'")]
    public static partial IGenericMessage InvalidPeriodComparisonType(ILogger logger, string periodType);

    [MessageLogging(EventId = 1532, Level = LogLevel.Warning, Message = "Failed to cache result: {error}")]
    public static partial IGenericMessage CacheFailed(ILogger logger, string error);
}
