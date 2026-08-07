using System.Diagnostics.CodeAnalysis;
using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Logging;

/// <summary>
/// MessageLogging definitions for ETL orchestration execution endpoints.
/// EventId range: 9300-9329
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class EtlExecutionLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Trigger operations (9300-9304)
    // ═══════════════════════════════════════════════════════════════════════════

    [MessageLogging(EventId = 9300, Level = LogLevel.Debug, Message = "Triggering ETL orchestration type '{type}' for node '{nodeId}'")]
    public static partial IGenericMessage TriggeringNode(ILogger logger, string type, Guid nodeId);

    [MessageLogging(EventId = 9301, Level = LogLevel.Information, Message = "ETL node '{nodeId}' queued — executionId='{executionId}'")]
    public static partial IGenericMessage NodeQueued(ILogger logger, Guid nodeId, Guid executionId);

    [MessageLogging(EventId = 9302, Level = LogLevel.Warning, Message = "ETL trigger queue full — node '{nodeId}' rejected")]
    public static partial IGenericMessage TriggerQueueFull(ILogger logger, Guid nodeId);

    [MessageLogging(EventId = 9303, Level = LogLevel.Warning, Message = "ETL trigger: node not found (type='{type}', id='{id}', name='{name}')")]
    public static partial IGenericMessage TriggerNodeNotFound(ILogger logger, string type, string id, string name);

    [MessageLogging(EventId = 9304, Level = LogLevel.Error, Message = "ETL trigger failed for type '{type}': {message}")]
    public static partial IGenericMessage TriggerFailed(ILogger logger, string type, string message);

    // ═══════════════════════════════════════════════════════════════════════════
    // Execution status operations (9305-9309)
    // ═══════════════════════════════════════════════════════════════════════════

    [MessageLogging(EventId = 9305, Level = LogLevel.Trace, Message = "Getting ETL execution status for '{executionId}'")]
    public static partial IGenericMessage GettingExecutionStatus(ILogger logger, Guid executionId);

    [MessageLogging(EventId = 9306, Level = LogLevel.Information, Message = "ETL execution status retrieved for '{executionId}'")]
    public static partial IGenericMessage ExecutionStatusRetrieved(ILogger logger, Guid executionId);

    [MessageLogging(EventId = 9307, Level = LogLevel.Warning, Message = "ETL execution '{executionId}' not found")]
    public static partial IGenericMessage ExecutionNotFound(ILogger logger, Guid executionId);

    [MessageLogging(EventId = 9308, Level = LogLevel.Error, Message = "Failed to get ETL execution status for '{executionId}': {message}")]
    public static partial IGenericMessage ExecutionStatusFailed(ILogger logger, Guid executionId, string message);

    // ═══════════════════════════════════════════════════════════════════════════
    // Cancel operations (9310-9314)
    // ═══════════════════════════════════════════════════════════════════════════

    [MessageLogging(EventId = 9310, Level = LogLevel.Debug, Message = "Cancelling ETL execution '{executionId}'")]
    public static partial IGenericMessage CancellingExecution(ILogger logger, Guid executionId);

    [MessageLogging(EventId = 9311, Level = LogLevel.Information, Message = "ETL execution '{executionId}' cancelled")]
    public static partial IGenericMessage ExecutionCancelled(ILogger logger, Guid executionId);

    [MessageLogging(EventId = 9312, Level = LogLevel.Warning, Message = "ETL execution '{executionId}' not found for cancel")]
    public static partial IGenericMessage ExecutionNotFoundForCancel(ILogger logger, Guid executionId);

    [MessageLogging(EventId = 9313, Level = LogLevel.Error, Message = "Failed to cancel ETL execution '{executionId}': {message}")]
    public static partial IGenericMessage ExecutionCancelFailed(ILogger logger, Guid executionId, string message);

    // ═══════════════════════════════════════════════════════════════════════════
    // Approve operations (9315-9319)
    // ═══════════════════════════════════════════════════════════════════════════

    [MessageLogging(EventId = 9315, Level = LogLevel.Debug, Message = "Approving ETL execution '{executionId}'")]
    public static partial IGenericMessage ApprovingExecution(ILogger logger, Guid executionId);

    [MessageLogging(EventId = 9316, Level = LogLevel.Information, Message = "ETL execution '{executionId}' approved and re-queued")]
    public static partial IGenericMessage ExecutionApproved(ILogger logger, Guid executionId);

    [MessageLogging(EventId = 9317, Level = LogLevel.Warning, Message = "ETL execution '{executionId}' not found for approval")]
    public static partial IGenericMessage ExecutionNotFoundForApproval(ILogger logger, Guid executionId);

    [MessageLogging(EventId = 9318, Level = LogLevel.Error, Message = "Failed to approve ETL execution '{executionId}': {message}")]
    public static partial IGenericMessage ExecutionApprovalFailed(ILogger logger, Guid executionId, string message);

    [MessageLogging(EventId = 9319, Level = LogLevel.Error, Message = "ETL execution '{executionId}' approval failed — missing rootNodeId parameter")]
    public static partial IGenericMessage ExecutionApprovalMissingRootNode(ILogger logger, Guid executionId);
}
