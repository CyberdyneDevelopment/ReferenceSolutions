using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Logging;

/// <summary>
/// MessageLogging definitions for Schedule management endpoints.
/// EventId range: 8300-8399
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class ScheduleLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Create operations (8300-8309)
    // ═══════════════════════════════════════════════════════════════════════════

    [MessageLogging(EventId = 8300, Level = LogLevel.Information, Message = "Creating schedule '{name}' for pipeline '{pipelineName}'")]
    public static partial IGenericMessage CreatingSchedule(ILogger logger, string name, string pipelineName);

    [MessageLogging(EventId = 8301, Level = LogLevel.Information, Message = "Schedule '{name}' created successfully")]
    public static partial IGenericMessage ScheduleCreated(ILogger logger, string name);

    [MessageLogging(EventId = 8302, Level = LogLevel.Error, Message = "Failed to create schedule '{name}': {message}")]
    public static partial IGenericMessage ScheduleCreateFailed(ILogger logger, string name, string message);

    [MessageLogging(EventId = 8303, Level = LogLevel.Warning, Message = "Schedule '{name}' already exists")]
    public static partial IGenericMessage ScheduleAlreadyExists(ILogger logger, string name);

    // ═══════════════════════════════════════════════════════════════════════════
    // Read operations (8310-8319)
    // ═══════════════════════════════════════════════════════════════════════════

    [MessageLogging(EventId = 8310, Level = LogLevel.Information, Message = "Fetching schedule '{name}'")]
    public static partial IGenericMessage FetchingSchedule(ILogger logger, string name);

    [MessageLogging(EventId = 8311, Level = LogLevel.Information, Message = "Schedule '{name}' retrieved")]
    public static partial IGenericMessage ScheduleRetrieved(ILogger logger, string name);

    [MessageLogging(EventId = 8312, Level = LogLevel.Warning, Message = "Schedule '{name}' not found")]
    public static partial IGenericMessage ScheduleNotFound(ILogger logger, string name);

    [MessageLogging(EventId = 8313, Level = LogLevel.Error, Message = "Failed to fetch schedule '{name}': {message}")]
    public static partial IGenericMessage ScheduleFetchFailed(ILogger logger, string name, string message);

    [MessageLogging(EventId = 8314, Level = LogLevel.Information, Message = "Listing schedules (page {page}, size {pageSize})")]
    public static partial IGenericMessage ListingSchedules(ILogger logger, int page, int pageSize);

    [MessageLogging(EventId = 8315, Level = LogLevel.Information, Message = "Found {count} schedules")]
    public static partial IGenericMessage SchedulesListed(ILogger logger, int count);

    [MessageLogging(EventId = 8316, Level = LogLevel.Error, Message = "Failed to list schedules: {message}")]
    public static partial IGenericMessage ScheduleListFailed(ILogger logger, string message);

    // ═══════════════════════════════════════════════════════════════════════════
    // Update operations (8320-8329)
    // ═══════════════════════════════════════════════════════════════════════════

    [MessageLogging(EventId = 8320, Level = LogLevel.Information, Message = "Updating schedule '{name}'")]
    public static partial IGenericMessage UpdatingSchedule(ILogger logger, string name);

    [MessageLogging(EventId = 8321, Level = LogLevel.Information, Message = "Schedule '{name}' updated successfully")]
    public static partial IGenericMessage ScheduleUpdated(ILogger logger, string name);

    [MessageLogging(EventId = 8322, Level = LogLevel.Error, Message = "Failed to update schedule '{name}': {message}")]
    public static partial IGenericMessage ScheduleUpdateFailed(ILogger logger, string name, string message);

    [MessageLogging(EventId = 8323, Level = LogLevel.Information, Message = "Schedule '{name}' enabled")]
    public static partial IGenericMessage ScheduleEnabled(ILogger logger, string name);

    [MessageLogging(EventId = 8324, Level = LogLevel.Information, Message = "Schedule '{name}' disabled")]
    public static partial IGenericMessage ScheduleDisabled(ILogger logger, string name);

    // ═══════════════════════════════════════════════════════════════════════════
    // Delete operations (8330-8339)
    // ═══════════════════════════════════════════════════════════════════════════

    [MessageLogging(EventId = 8330, Level = LogLevel.Information, Message = "Deleting schedule '{name}'")]
    public static partial IGenericMessage DeletingSchedule(ILogger logger, string name);

    [MessageLogging(EventId = 8331, Level = LogLevel.Information, Message = "Schedule '{name}' deleted successfully")]
    public static partial IGenericMessage ScheduleDeleted(ILogger logger, string name);

    [MessageLogging(EventId = 8332, Level = LogLevel.Error, Message = "Failed to delete schedule '{name}': {message}")]
    public static partial IGenericMessage ScheduleDeleteFailed(ILogger logger, string name, string message);

    // ═══════════════════════════════════════════════════════════════════════════
    // Validation operations (8340-8349)
    // ═══════════════════════════════════════════════════════════════════════════

    [MessageLogging(EventId = 8340, Level = LogLevel.Warning, Message = "Validation failed for schedule '{name}': {message}")]
    public static partial IGenericMessage ScheduleValidationFailed(ILogger logger, string name, string message);

    [MessageLogging(EventId = 8341, Level = LogLevel.Warning, Message = "Unknown scheduler type '{schedulerType}'")]
    public static partial IGenericMessage UnknownSchedulerType(ILogger logger, string schedulerType);

    [MessageLogging(EventId = 8342, Level = LogLevel.Warning, Message = "Invalid cron expression '{cronExpression}' for schedule '{name}'")]
    public static partial IGenericMessage InvalidCronExpression(ILogger logger, string cronExpression, string name);

    [MessageLogging(EventId = 8343, Level = LogLevel.Warning, Message = "Pipeline '{pipelineName}' not found for schedule '{name}'")]
    public static partial IGenericMessage PipelineNotFoundForSchedule(ILogger logger, string pipelineName, string name);

    [MessageLogging(EventId = 8344, Level = LogLevel.Warning, Message = "Cron expression required for Cron scheduler type on schedule '{name}'")]
    public static partial IGenericMessage CronExpressionRequired(ILogger logger, string name);

    [MessageLogging(EventId = 8345, Level = LogLevel.Warning, Message = "Interval seconds required for Interval scheduler type on schedule '{name}'")]
    public static partial IGenericMessage IntervalSecondsRequired(ILogger logger, string name);
}
