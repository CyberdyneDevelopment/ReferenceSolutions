using System.Diagnostics.CodeAnalysis;
using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Logging;

/// <summary>
/// MessageLogging definitions for Execution tracking endpoints.
/// EventId range: 8400-8499
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class ExecutionLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Trigger operations (8400-8409)
    // ═══════════════════════════════════════════════════════════════════════════

    [MessageLogging(EventId = 8400, Level = LogLevel.Information, Message = "Triggering workflow '{workflowName}' with correlation ID '{correlationId}'")]
    public static partial IGenericMessage TriggeringWorkflow(ILogger logger, string workflowName, string correlationId);

    [MessageLogging(EventId = 8401, Level = LogLevel.Information, Message = "Workflow '{workflowName}' triggered successfully with execution ID '{executionId}'")]
    public static partial IGenericMessage WorkflowTriggered(ILogger logger, string workflowName, Guid executionId);

    [MessageLogging(EventId = 8402, Level = LogLevel.Error, Message = "Failed to trigger workflow '{workflowName}': {message}")]
    public static partial IGenericMessage WorkflowTriggerFailed(ILogger logger, string workflowName, string message);

    [MessageLogging(EventId = 8403, Level = LogLevel.Warning, Message = "Workflow '{workflowName}' not found")]
    public static partial IGenericMessage WorkflowNotFound(ILogger logger, string workflowName);

    [MessageLogging(EventId = 8404, Level = LogLevel.Information, Message = "Dry run requested for workflow '{workflowName}'")]
    public static partial IGenericMessage DryRunRequested(ILogger logger, string workflowName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Read operations (8410-8419)
    // ═══════════════════════════════════════════════════════════════════════════

    [MessageLogging(EventId = 8410, Level = LogLevel.Information, Message = "Fetching execution '{executionId}'")]
    public static partial IGenericMessage FetchingExecution(ILogger logger, Guid executionId);

    [MessageLogging(EventId = 8411, Level = LogLevel.Information, Message = "Execution '{executionId}' retrieved")]
    public static partial IGenericMessage ExecutionRetrieved(ILogger logger, Guid executionId);

    [MessageLogging(EventId = 8412, Level = LogLevel.Warning, Message = "Execution '{executionId}' not found")]
    public static partial IGenericMessage ExecutionNotFound(ILogger logger, Guid executionId);

    [MessageLogging(EventId = 8413, Level = LogLevel.Error, Message = "Failed to fetch execution '{executionId}': {message}")]
    public static partial IGenericMessage ExecutionFetchFailed(ILogger logger, Guid executionId, string message);

    [MessageLogging(EventId = 8414, Level = LogLevel.Information, Message = "Listing executions (page {page}, size {pageSize})")]
    public static partial IGenericMessage ListingExecutions(ILogger logger, int page, int pageSize);

    [MessageLogging(EventId = 8415, Level = LogLevel.Information, Message = "Found {count} executions")]
    public static partial IGenericMessage ExecutionsListed(ILogger logger, int count);

    [MessageLogging(EventId = 8416, Level = LogLevel.Information, Message = "Querying executions by correlation ID '{correlationId}'")]
    public static partial IGenericMessage QueryingByCorrelationId(ILogger logger, string correlationId);

    [MessageLogging(EventId = 8417, Level = LogLevel.Error, Message = "Failed to query executions by correlation ID: {message}")]
    public static partial IGenericMessage ExecutionQueryFailed(ILogger logger, string message);

    // ═══════════════════════════════════════════════════════════════════════════
    // Event operations (8420-8429)
    // ═══════════════════════════════════════════════════════════════════════════

    [MessageLogging(EventId = 8420, Level = LogLevel.Information, Message = "Fetching events for execution '{executionId}'")]
    public static partial IGenericMessage FetchingEvents(ILogger logger, Guid executionId);

    [MessageLogging(EventId = 8421, Level = LogLevel.Information, Message = "Found {count} events for execution '{executionId}'")]
    public static partial IGenericMessage EventsRetrieved(ILogger logger, Guid executionId, int count);

    [MessageLogging(EventId = 8422, Level = LogLevel.Error, Message = "Failed to fetch events for execution '{executionId}': {message}")]
    public static partial IGenericMessage EventsFetchFailed(ILogger logger, Guid executionId, string message);

    // ═══════════════════════════════════════════════════════════════════════════
    // State transition operations (8430-8439)
    // ═══════════════════════════════════════════════════════════════════════════

    [MessageLogging(EventId = 8430, Level = LogLevel.Information, Message = "Cancelling execution '{executionId}'")]
    public static partial IGenericMessage CancellingExecution(ILogger logger, Guid executionId);

    [MessageLogging(EventId = 8431, Level = LogLevel.Information, Message = "Execution '{executionId}' cancelled successfully")]
    public static partial IGenericMessage ExecutionCancelled(ILogger logger, Guid executionId);

    [MessageLogging(EventId = 8432, Level = LogLevel.Error, Message = "Failed to cancel execution '{executionId}': {message}")]
    public static partial IGenericMessage ExecutionCancelFailed(ILogger logger, Guid executionId, string message);

    [MessageLogging(EventId = 8433, Level = LogLevel.Information, Message = "Pausing execution '{executionId}'")]
    public static partial IGenericMessage PausingExecution(ILogger logger, Guid executionId);

    [MessageLogging(EventId = 8434, Level = LogLevel.Information, Message = "Execution '{executionId}' paused successfully")]
    public static partial IGenericMessage ExecutionPaused(ILogger logger, Guid executionId);

    [MessageLogging(EventId = 8435, Level = LogLevel.Error, Message = "Failed to pause execution '{executionId}': {message}")]
    public static partial IGenericMessage ExecutionPauseFailed(ILogger logger, Guid executionId, string message);

    [MessageLogging(EventId = 8436, Level = LogLevel.Information, Message = "Resuming execution '{executionId}'")]
    public static partial IGenericMessage ResumingExecution(ILogger logger, Guid executionId);

    [MessageLogging(EventId = 8437, Level = LogLevel.Information, Message = "Execution '{executionId}' resumed successfully")]
    public static partial IGenericMessage ExecutionResumed(ILogger logger, Guid executionId);

    [MessageLogging(EventId = 8438, Level = LogLevel.Error, Message = "Failed to resume execution '{executionId}': {message}")]
    public static partial IGenericMessage ExecutionResumeFailed(ILogger logger, Guid executionId, string message);

    [MessageLogging(EventId = 8439, Level = LogLevel.Warning, Message = "Execution '{executionId}' is already in terminal state '{state}'")]
    public static partial IGenericMessage ExecutionAlreadyTerminal(ILogger logger, Guid executionId, string state);

    // ═══════════════════════════════════════════════════════════════════════════
    // Children operations (8440-8449)
    // ═══════════════════════════════════════════════════════════════════════════

    [MessageLogging(EventId = 8440, Level = LogLevel.Information, Message = "Fetching children for execution '{executionId}'")]
    public static partial IGenericMessage FetchingChildren(ILogger logger, Guid executionId);

    [MessageLogging(EventId = 8441, Level = LogLevel.Information, Message = "Found {count} children for execution '{executionId}'")]
    public static partial IGenericMessage ChildrenRetrieved(ILogger logger, Guid executionId, int count);

    [MessageLogging(EventId = 8442, Level = LogLevel.Error, Message = "Failed to fetch children for execution '{executionId}': {message}")]
    public static partial IGenericMessage ChildrenFetchFailed(ILogger logger, Guid executionId, string message);
}
