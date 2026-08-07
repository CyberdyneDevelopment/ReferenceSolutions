using System.Diagnostics.CodeAnalysis;
#pragma warning disable CS1591
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Reference.Api.Logging;

/// <summary>
/// MessageLogging for Bulk operations.
/// EventId range: 1860-1879
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class BulkLog
{
    /// <summary>
    /// Logs when a bulk export operation begins.
    /// </summary>
    [MessageLogging(
        EventId = 1860,
        Level = LogLevel.Information,
        Message = "Starting bulk export")]
    public static partial IGenericMessage BulkExportStarting(ILogger logger);

    /// <summary>
    /// Logs when a bulk export operation completes.
    /// </summary>
    [MessageLogging(
        EventId = 1861,
        Level = LogLevel.Information,
        Message = "Bulk export completed: {summary}")]
    public static partial IGenericMessage BulkExportCompleted(ILogger logger, IDictionary<string, int> summary);

    /// <summary>
    /// Logs when pipeline export fails.
    /// </summary>
    [MessageLogging(
        EventId = 1862,
        Level = LogLevel.Warning,
        Message = "Failed to export pipelines from database")]
    public static partial IGenericMessage PipelineExportFailed(ILogger logger, Exception exception);

    /// <summary>
    /// Logs when dataset export fails.
    /// </summary>
    [MessageLogging(
        EventId = 1863,
        Level = LogLevel.Warning,
        Message = "Failed to export datasets from database")]
    public static partial IGenericMessage DataSetExportFailed(ILogger logger, Exception exception);

    /// <summary>
    /// Logs when schedule export fails.
    /// </summary>
    [MessageLogging(
        EventId = 1864,
        Level = LogLevel.Warning,
        Message = "Failed to export schedules from database")]
    public static partial IGenericMessage ScheduleExportFailed(ILogger logger, Exception exception);

    /// <summary>
    /// Logs when a bulk import operation begins.
    /// </summary>
    [MessageLogging(
        EventId = 1865,
        Level = LogLevel.Information,
        Message = "Starting bulk import")]
    public static partial IGenericMessage BulkImportStarting(ILogger logger);

    /// <summary>
    /// Logs when a bulk import operation completes.
    /// </summary>
    [MessageLogging(
        EventId = 1866,
        Level = LogLevel.Information,
        Message = "Bulk import completed: Created={created}, Updated={updated}, Skipped={skipped}, Errors={errorCount}")]
    public static partial IGenericMessage BulkImportCompleted(
        ILogger logger,
        IDictionary<string, int> created,
        IDictionary<string, int> updated,
        IDictionary<string, int> skipped,
        int errorCount);
}
