using System.Diagnostics.CodeAnalysis;
#pragma warning disable CS1591
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Reference.Api.Logging;

/// <summary>
/// MessageLogging for FieldMapping operations.
/// EventId range: 1830-1849
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class FieldMappingLog
{
    /// <summary>
    /// Logs when retrieving all field mappings for a DataSet.
    /// </summary>
    [MessageLogging(
        EventId = 1830,
        Level = LogLevel.Information,
        Message = "Retrieved {count} mappings for DataSet {dataSetName}")]
    public static partial IGenericMessage MappingsRetrieved(ILogger logger, int count, string dataSetName);

    /// <summary>
    /// Logs when getting mappings for a specific DataSet and source.
    /// </summary>
    [MessageLogging(
        EventId = 1831,
        Level = LogLevel.Information,
        Message = "Getting mappings for DataSet {dataSetName}, source {sourceName}")]
    public static partial IGenericMessage GettingSourceMappings(ILogger logger, string dataSetName, string sourceName);

    /// <summary>
    /// Logs when a source is not found for a DataSet.
    /// </summary>
    [MessageLogging(
        EventId = 1832,
        Level = LogLevel.Warning,
        Message = "Source {sourceName} not found for DataSet {dataSetName}")]
    public static partial IGenericMessage SourceNotFound(ILogger logger, string sourceName, string dataSetName);

    /// <summary>
    /// Logs when mappings have been retrieved for a source.
    /// </summary>
    [MessageLogging(
        EventId = 1833,
        Level = LogLevel.Information,
        Message = "Retrieved {count} mappings for source {sourceName}")]
    public static partial IGenericMessage SourceMappingsRetrieved(ILogger logger, int count, string sourceName);

    /// <summary>
    /// Logs when saving field mappings for a source.
    /// </summary>
    [MessageLogging(
        EventId = 1834,
        Level = LogLevel.Information,
        Message = "Saving {count} mappings for DataSet {dataSetName}, source {sourceName}")]
    public static partial IGenericMessage SavingMappings(ILogger logger, int count, string dataSetName, string sourceName);

    /// <summary>
    /// Logs when mappings have been successfully saved.
    /// </summary>
    [MessageLogging(
        EventId = 1835,
        Level = LogLevel.Information,
        Message = "Saved {count} mappings for source {sourceName}")]
    public static partial IGenericMessage MappingsSaved(ILogger logger, int count, string sourceName);

    /// <summary>
    /// Logs when validating field mappings.
    /// </summary>
    [MessageLogging(
        EventId = 1836,
        Level = LogLevel.Information,
        Message = "Validating {count} mappings for DataSet {dataSetName}")]
    public static partial IGenericMessage ValidatingMappings(ILogger logger, int count, string dataSetName);

    /// <summary>
    /// Logs when validation completes.
    /// </summary>
    [MessageLogging(
        EventId = 1837,
        Level = LogLevel.Information,
        Message = "Validation complete: {errorCount} errors, {warningCount} warnings")]
    public static partial IGenericMessage ValidationCompleted(ILogger logger, int errorCount, int warningCount);
}
