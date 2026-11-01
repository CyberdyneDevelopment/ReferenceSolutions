using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceSchedulers.EtlDispatch.Logging;

/// <summary>
/// MessageLogging for <see cref="EtlDispatchExecutionHandler"/>.
/// </summary>
/// <remarks>
/// Numbers follow the result-code categories: 1xxxx progress, 2xxxx bad input, 3xxxx not found,
/// 4xxxx state, 7xxxx a failed call to the ETL server.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "MessageLogging partial class - implementation is source-generated")]
[MessageLoggingTypeCode("REFETLDISPATCH")]
public static partial class EtlDispatchLog
{
    /// <summary>Traces a schedule fire being dispatched to the ETL server.</summary>
    [MessageLogging(EventId = 10001, Level = LogLevel.Trace,
        Message = "Dispatching pipeline '{processId}' for schedule '{scheduleId}' (scheduled {scheduledTime:o}, fired {actualTime:o})")]
    public static partial IGenericMessage Dispatching(ILogger logger, string processId, string scheduleId, DateTime scheduledTime, DateTime actualTime);

    /// <summary>Records the ETL server accepting the triggered pipeline.</summary>
    // Why Information: one line per scheduled run is the operator's record that a schedule actually fired.
    [MessageLogging(EventId = 10002, Level = LogLevel.Information,
        Message = "Pipeline '{processId}' for schedule '{scheduleId}' queued on the ETL server as execution '{executionId}'")]
    public static partial IGenericMessage Queued(ILogger logger, string processId, string scheduleId, Guid executionId);

    /// <summary>Traces the answer to whether a process is dispatchable.</summary>
    [MessageLogging(EventId = 10003, Level = LogLevel.Trace,
        Message = "Process '{processId}' can be dispatched: {canHandle}")]
    public static partial IGenericMessage CanHandle(ILogger logger, string processId, bool canHandle);

    /// <summary>A fire arrived with no process (pipeline) name.</summary>
    [MessageLogging(EventId = 20000, Level = LogLevel.Error,
        Message = "Schedule '{scheduleId}' fired with no process id; the pipeline to trigger is unknown")]
    public static partial IGenericMessage ProcessIdMissing(ILogger logger, string scheduleId);

    /// <summary>A fire arrived with no schedule name.</summary>
    [MessageLogging(EventId = 20001, Level = LogLevel.Error,
        Message = "Pipeline '{processId}' fired with no schedule id; the schedule, and so its tenant, is unknown")]
    public static partial IGenericMessage ScheduleIdMissing(ILogger logger, string processId);

    /// <summary>The firing schedule's configuration could not be read.</summary>
    [MessageLogging(EventId = 30000, Level = LogLevel.Error,
        Message = "Schedule '{scheduleId}' could not be read, so pipeline '{processId}' was not triggered: {reason}")]
    public static partial IGenericMessage ScheduleUnreadable(ILogger logger, string scheduleId, string processId, string? reason);

    /// <summary>Process information was asked of a handler that holds none.</summary>
    [MessageLogging(EventId = 40000, Level = LogLevel.Warning,
        Message = "Process information for '{processId}' is not held here: this handler triggers pipelines by name on the ETL server, which owns their definition")]
    public static partial IGenericMessage ProcessInfoNotHeld(ILogger logger, string processId);

    /// <summary>The ETL server refused or could not be reached for the trigger.</summary>
    [MessageLogging(EventId = 70000, Level = LogLevel.Error,
        Message = "Triggering pipeline '{processId}' for schedule '{scheduleId}' failed: {reason}")]
    public static partial IGenericMessage TriggerFailed(ILogger logger, string processId, string scheduleId, string? reason);

    /// <summary>A scheduled job's process id is not a pipeline id, so it names no pipeline to run.</summary>
    [MessageLogging(EventId = 20002, Level = LogLevel.Warning,
        Message = "Schedule '{scheduleId}' fired with process '{processId}', which is not a pipeline id")]
    public static partial IGenericMessage ProcessIdNotPipelineId(ILogger logger, string processId, string scheduleId);

    /// <summary>A scheduled job still holds a pipeline the schedule no longer names; it is not run.</summary>
    [MessageLogging(EventId = 20003, Level = LogLevel.Warning,
        Message = "Schedule '{scheduleId}' fired for pipeline '{processId}' but now names pipeline '{pipelineId}'; the stale job is not run")]
    public static partial IGenericMessage ProcessIdStale(ILogger logger, string processId, string scheduleId, Guid pipelineId);
}
