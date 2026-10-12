using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceSchedulers.Quartz.Logging;

/// <summary>
/// MessageLogging for Quartz scheduler operations.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "MessageLogging partial class - implementation is source-generated")]
[MessageLoggingTypeCode("REFQUARTZ")]
public static partial class QuartzSchedulingLog
{
    /// <summary>Traces a scheduling command about to be translated and executed against Quartz.</summary>
    [MessageLogging(EventId = 1, Level = LogLevel.Trace, Message = "Translating '{commandType}' command for schedule '{scheduleId}' against Quartz")]
    public static partial IGenericMessage TranslatingCommand(ILogger logger, string commandType, string scheduleId);

    /// <summary>Logs a Quartz job execution failing.</summary>
    [MessageLogging(EventId = 2, Level = LogLevel.Error, Message = "Scheduled job for schedule '{scheduleId}' (process '{processId}') failed: {error}")]
    public static partial IGenericMessage JobExecutionFailed(ILogger logger, string scheduleId, string processId, string? error);

    /// <summary>Logs a Quartz native call throwing.</summary>
    [MessageLogging(EventId = 3, Level = LogLevel.Error, Message = "Quartz call for schedule '{scheduleId}' failed")]
    public static partial IGenericMessage NativeCallFailed(ILogger logger, Exception ex, string scheduleId);

    /// <summary>Traces a Quartz trigger built from a schedule's trigger kind.</summary>
    [MessageLogging(EventId = 4, Level = LogLevel.Trace, Message = "Built a {triggerType} Quartz trigger for schedule '{scheduleId}'")]
    public static partial IGenericMessage TriggerBuilt(ILogger logger, string scheduleId, string triggerType);

    /// <summary>A schedule's trigger kind is missing the value it needs to be scheduled.</summary>
    [MessageLogging(EventId = 5, Level = LogLevel.Error, Message = "Schedule '{scheduleId}' is a {triggerType} schedule but has no usable {field}, so it cannot be scheduled")]
    public static partial IGenericMessage TriggerDataMissing(ILogger logger, string scheduleId, string triggerType, string field);

    /// <summary>A schedule does not fire on a clock, so Quartz does not register it.</summary>
    // Why Debug: reconciliation meets the same row every pass (once a minute), so a louder level would
    // repeat the same line all day; the row is skipped by design, not by failure.
    [MessageLogging(EventId = 6, Level = LogLevel.Debug, Message = "Schedule '{scheduleId}' is a {triggerType} schedule, which does not fire on a clock; Quartz does not register it")]
    public static partial IGenericMessage ScheduleNotClockDriven(ILogger logger, string scheduleId, string triggerType);

    /// <summary>Traces a fire's run fields written back to the schedule row.</summary>
    [MessageLogging(EventId = 7, Level = LogLevel.Trace, Message = "Schedule '{scheduleId}' run recorded: status {status}, next run {nextRunTime}")]
    public static partial IGenericMessage RunFieldsWritten(ILogger logger, string scheduleId, string status, DateTimeOffset? nextRunTime);

    /// <summary>Writing a fire's run fields back to the schedule row failed.</summary>
    [MessageLogging(EventId = 8, Level = LogLevel.Error, Message = "Schedule '{scheduleId}' run fields could not be written back: {reason}")]
    public static partial IGenericMessage RunFieldsWriteFailed(ILogger logger, string scheduleId, string? reason);

    /// <summary>A fired job carries no value for a key its translator always writes.</summary>
    [MessageLogging(EventId = 9, Level = LogLevel.Error, Message = "Fired Quartz job '{jobKey}' has no '{dataKey}' in its job data")]
    public static partial IGenericMessage JobDataMissing(ILogger logger, string jobKey, string dataKey);

    /// <summary>Writing a fire's execution history row failed.</summary>
    [MessageLogging(EventId = 10, Level = LogLevel.Error, Message = "Schedule '{scheduleId}' execution history could not be written: {reason}")]
    public static partial IGenericMessage HistoryWriteFailed(ILogger logger, string scheduleId, string? reason);

    /// <summary>A live Quartz trigger is of a kind no schedule translates to.</summary>
    [MessageLogging(EventId = 11, Level = LogLevel.Error, Message = "Live Quartz trigger for schedule '{scheduleId}' is a {triggerClass}, which no schedule kind maps to")]
    public static partial IGenericMessage UnrecognisedTrigger(ILogger logger, string scheduleId, string triggerClass);

    /// <summary>The handler reported success but returned no process result, so the run is recorded as Failed.</summary>
    [MessageLogging(EventId = 12, Level = LogLevel.Error, Message = "Schedule '{scheduleId}' (process '{processId}'): the scheduled execution handler reported success but returned no process result; the run is recorded as Failed")]
    public static partial IGenericMessage HandlerReturnedNoResult(ILogger logger, string scheduleId, string processId);
}
