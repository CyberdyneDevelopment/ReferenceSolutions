using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceSchedulers.Hangfire.Logging;

/// <summary>
/// MessageLogging for Hangfire scheduler operations.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "MessageLogging partial class - implementation is source-generated")]
[MessageLoggingTypeCode("REFHANGFIRE")]
public static partial class HangfireSchedulingLog
{
    /// <summary>Traces a scheduling command about to be translated and executed against Hangfire.</summary>
    [MessageLogging(EventId = 1, Level = LogLevel.Trace, Message = "Translating '{commandType}' command for schedule '{scheduleId}' against Hangfire")]
    public static partial IGenericMessage TranslatingCommand(ILogger logger, string commandType, string scheduleId);

    /// <summary>Logs a Hangfire recurring job execution failing.</summary>
    [MessageLogging(EventId = 2, Level = LogLevel.Error, Message = "Scheduled job for schedule '{scheduleId}' (process '{processId}') failed: {error}")]
    public static partial IGenericMessage JobExecutionFailed(ILogger logger, string scheduleId, string processId, string? error);

    /// <summary>Logs a Hangfire native call throwing.</summary>
    [MessageLogging(EventId = 3, Level = LogLevel.Error, Message = "Hangfire call for schedule '{scheduleId}' failed")]
    public static partial IGenericMessage NativeCallFailed(ILogger logger, Exception ex, string scheduleId);

    /// <summary>A schedule is not a Cron schedule, so Hangfire does not register it.</summary>
    // Why Debug: reconciliation meets the same row every pass, and the skip is by design -- Hangfire's
    // recurring jobs are cron-only, so an Interval, Once, Window, Event or Manual schedule has no
    // recurring-job form here.
    [MessageLogging(EventId = 4, Level = LogLevel.Debug, Message = "Schedule '{scheduleId}' is a {triggerType} schedule; Hangfire recurring jobs are cron-only, so it is not registered")]
    public static partial IGenericMessage NotCronSchedule(ILogger logger, string scheduleId, string triggerType);

    /// <summary>A Cron schedule reached a translator without a cron expression.</summary>
    [MessageLogging(EventId = 5, Level = LogLevel.Error, Message = "Schedule '{scheduleId}' has no cron expression, so Hangfire cannot register it")]
    public static partial IGenericMessage CronExpressionMissing(ILogger logger, string scheduleId);

    /// <summary>The handler reported success but returned no process result, so the run is recorded as Failed.</summary>
    [MessageLogging(EventId = 6, Level = LogLevel.Error, Message = "Schedule '{scheduleId}' (process '{processId}'): the scheduled execution handler reported success but returned no process result; the run is recorded as Failed")]
    public static partial IGenericMessage HandlerReturnedNoResult(ILogger logger, string scheduleId, string processId);
}
