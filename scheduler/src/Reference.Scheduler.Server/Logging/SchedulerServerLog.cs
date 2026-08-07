using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Reference.Scheduler.Server.Logging;

/// <summary>
/// MessageLogging for Scheduler Server operations.
/// EventId range: 11100-11199 (legacy), 8200-8299 (new scheduler domain methods)
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class SchedulerServerLog
{
    // Legacy scheduler events (11100-11199)

    [MessageLogging(
        EventId = 11100,
        Level = LogLevel.Information,
        Message = "Scheduler started with poll interval {intervalSeconds}s")]
    public static partial IGenericMessage SchedulerStarted(ILogger logger, int intervalSeconds);

    [MessageLogging(
        EventId = 11101,
        Level = LogLevel.Information,
        Message = "Scheduler stopped")]
    public static partial IGenericMessage SchedulerStopped(ILogger logger);

    [MessageLogging(
        EventId = 11110,
        Level = LogLevel.Debug,
        Message = "Checking for due jobs")]
    public static partial IGenericMessage SchedulerPollStarted(ILogger logger);

    [MessageLogging(
        EventId = 11111,
        Level = LogLevel.Debug,
        Message = "Poll completed, {dueJobCount} jobs due")]
    public static partial IGenericMessage SchedulerPollCompleted(ILogger logger, int dueJobCount);

    [MessageLogging(
        EventId = 11120,
        Level = LogLevel.Information,
        Message = "Job '{jobId}' scheduled for execution at {scheduledTime}")]
    public static partial IGenericMessage JobScheduled(ILogger logger, string jobId, DateTimeOffset scheduledTime);

    [MessageLogging(
        EventId = 11121,
        Level = LogLevel.Information,
        Message = "Job '{jobId}' is due, dispatching")]
    public static partial IGenericMessage JobDue(ILogger logger, string jobId);

    [MessageLogging(
        EventId = 11122,
        Level = LogLevel.Information,
        Message = "Job '{jobId}' dispatched to ETL server")]
    public static partial IGenericMessage JobDispatched(ILogger logger, string jobId);

    [MessageLogging(
        EventId = 11123,
        Level = LogLevel.Error,
        Message = "Failed to dispatch job '{jobId}': {error}")]
    public static partial IGenericMessage JobDispatchFailed(ILogger logger, Exception ex, string jobId, string error);

    [MessageLogging(
        EventId = 11130,
        Level = LogLevel.Information,
        Message = "Schedule '{scheduleName}' created with cron '{cronExpression}'")]
    public static partial IGenericMessage ScheduleCreated(ILogger logger, string scheduleName, string cronExpression);

    [MessageLogging(
        EventId = 11131,
        Level = LogLevel.Information,
        Message = "Schedule '{scheduleName}' updated")]
    public static partial IGenericMessage ScheduleUpdated(ILogger logger, string scheduleName);

    [MessageLogging(
        EventId = 11132,
        Level = LogLevel.Information,
        Message = "Schedule '{scheduleName}' deleted")]
    public static partial IGenericMessage ScheduleDeleted(ILogger logger, string scheduleName);

    // New scheduler domain methods (8200-8299)
    // NOTE: EventIds 8200-8215 intentionally share range with framework SchedulingLog (8201-8264).
    // These are in separate assemblies (Reference.Scheduler.Server vs Fdw.Services.Scheduling)
    // and are distinguished by their TypeCode prefix in structured logs.

    [MessageLogging(
        EventId = 8200,
        Level = LogLevel.Information,
        Message = "Evaluation started for {scheduleCount} schedules")]
    public static partial IGenericMessage EvaluationStarted(ILogger logger, int scheduleCount);

    [MessageLogging(
        EventId = 8201,
        Level = LogLevel.Information,
        Message = "Evaluation completed: {schedulesProcessed} schedules processed, {jobsDispatched} jobs dispatched")]
    public static partial IGenericMessage EvaluationCompleted(ILogger logger, int schedulesProcessed, int jobsDispatched);

    [MessageLogging(
        EventId = 8202,
        Level = LogLevel.Debug,
        Message = "Evaluating schedule '{scheduleName}'")]
    public static partial IGenericMessage ScheduleEvaluating(ILogger logger, string scheduleName);

    [MessageLogging(
        EventId = 8203,
        Level = LogLevel.Information,
        Message = "Schedule '{scheduleName}' is due, dispatching pipeline '{pipelineName}'")]
    public static partial IGenericMessage ScheduleIsDue(ILogger logger, string scheduleName, string pipelineName);

    [MessageLogging(
        EventId = 8204,
        Level = LogLevel.Debug,
        Message = "Schedule '{scheduleName}' is not due")]
    public static partial IGenericMessage ScheduleNotDue(ILogger logger, string scheduleName);

    [MessageLogging(
        EventId = 8205,
        Level = LogLevel.Information,
        Message = "Dispatch started for schedule '{scheduleName}', pipeline '{pipelineName}'")]
    public static partial IGenericMessage DispatchStarted(ILogger logger, string scheduleName, string pipelineName);

    [MessageLogging(
        EventId = 8206,
        Level = LogLevel.Information,
        Message = "Dispatch completed for schedule '{scheduleName}', pipeline '{pipelineName}'")]
    public static partial IGenericMessage DispatchCompleted(ILogger logger, string scheduleName, string pipelineName);

    [MessageLogging(
        EventId = 8207,
        Level = LogLevel.Error,
        Message = "Dispatch failed for schedule '{scheduleName}', pipeline '{pipelineName}'")]
    public static partial IGenericMessage DispatchFailed(ILogger logger, Exception exception, string scheduleName, string pipelineName);

    [MessageLogging(
        EventId = 8208,
        Level = LogLevel.Error,
        Message = "Evaluator not found for scheduler type '{schedulerType}'")]
    public static partial IGenericMessage EvaluatorNotFound(ILogger logger, string schedulerType);

    [MessageLogging(
        EventId = 8209,
        Level = LogLevel.Error,
        Message = "Evaluation loop error")]
    public static partial IGenericMessage EvaluationLoopError(ILogger logger, Exception exception);

    [MessageLogging(
        EventId = 8210,
        Level = LogLevel.Error,
        Message = "Schedule update failed for '{scheduleName}'")]
    public static partial IGenericMessage ScheduleUpdateFailed(ILogger logger, Exception exception, string scheduleName);

    [MessageLogging(
        EventId = 8211,
        Level = LogLevel.Error,
        Message = "Schedule query failed")]
    public static partial IGenericMessage ScheduleQueryFailed(ILogger logger, Exception exception);

    [MessageLogging(
        EventId = 8212,
        Level = LogLevel.Warning,
        Message = "Schedule '{scheduleName}' not found")]
    public static partial IGenericMessage ScheduleNotFound(ILogger logger, string scheduleName);

    [MessageLogging(
        EventId = 8213,
        Level = LogLevel.Error,
        Message = "Schedule creation failed for '{scheduleName}'")]
    public static partial IGenericMessage ScheduleCreateFailed(ILogger logger, Exception exception, string scheduleName);

    [MessageLogging(
        EventId = 8214,
        Level = LogLevel.Error,
        Message = "Schedule deletion failed for '{scheduleName}'")]
    public static partial IGenericMessage ScheduleDeleteFailed(ILogger logger, Exception exception, string scheduleName);

    [MessageLogging(
        EventId = 8215,
        Level = LogLevel.Debug,
        Message = "Schedules loaded: {count} schedules")]
    public static partial IGenericMessage SchedulesLoaded(ILogger logger, int count);

    [MessageLogging(
        EventId = 11135,
        Level = LogLevel.Information,
        Message = "ETL dispatch disabled, skipping schedule '{scheduleName}'")]
    public static partial IGenericMessage DispatchDisabled(ILogger logger, string scheduleName);

    [MessageLogging(
        EventId = 11142,
        Level = LogLevel.Warning,
        Message = "Schedule '{scheduleName}' skipped, already in-flight")]
    public static partial IGenericMessage ScheduleAlreadyInFlight(ILogger logger, string scheduleName);

    // Endpoint trace events (11150-11159)

    [MessageLogging(
        EventId = 11150,
        Level = LogLevel.Trace,
        Message = "List schedules request received")]
    public static partial IGenericMessage ListSchedulesRequestReceived(ILogger logger);

    [MessageLogging(
        EventId = 11151,
        Level = LogLevel.Trace,
        Message = "Get schedule request received for '{scheduleName}'")]
    public static partial IGenericMessage GetScheduleRequestReceived(ILogger logger, string scheduleName);

    [MessageLogging(
        EventId = 11152,
        Level = LogLevel.Trace,
        Message = "Create schedule request received for '{scheduleName}'")]
    public static partial IGenericMessage CreateScheduleRequestReceived(ILogger logger, string scheduleName);

    [MessageLogging(
        EventId = 11153,
        Level = LogLevel.Trace,
        Message = "Update schedule request received for '{scheduleName}'")]
    public static partial IGenericMessage UpdateScheduleRequestReceived(ILogger logger, string scheduleName);

    [MessageLogging(
        EventId = 11154,
        Level = LogLevel.Trace,
        Message = "Delete schedule request received for '{scheduleName}'")]
    public static partial IGenericMessage DeleteScheduleRequestReceived(ILogger logger, string scheduleName);

    [MessageLogging(
        EventId = 8217,
        Level = LogLevel.Critical,
        Message = "Scheduler configuration not loaded from database. " +
            "Ensure cfg.Scheduler has at least one row with IsCurrent=1")]
    public static partial IGenericMessage SchedulerConfigurationNotLoaded(ILogger logger);

    [MessageLogging(
        EventId = 8216,
        Level = LogLevel.Error,
        Message = "Operation failed with no error message")]
    public static partial IGenericMessage OperationFailedNoMessage(ILogger logger);

    /// <summary>
    /// Returns the error message from a result, logging a warning if CurrentMessage is null.
    /// </summary>
    public static string GetError(Fdw.Results.IGenericResult result, ILogger logger)
    {
        if (result.CurrentMessage != null)
        {
            return result.CurrentMessage;
        }

        OperationFailedNoMessage(logger);
        return "Operation failed with no error message";
    }
}
