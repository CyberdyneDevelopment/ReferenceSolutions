using System.Diagnostics.CodeAnalysis;
using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace ReferencePipelines.Endpoints.Logging;

/// <summary>
/// MessageLogging definitions for Pipeline management endpoints.
/// EventId range: 8150-8249
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class PipelineLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Execution History operations (8150-8169)
    // ═══════════════════════════════════════════════════════════════════════════

    [MessageLogging(EventId = 8150, Level = LogLevel.Trace, Message = "Recording execution start for pipeline '{pipelineName}' with execution ID '{executionId}'")]
    public static partial IGenericMessage RecordingExecutionStart(ILogger logger, string pipelineName, Guid executionId);

    [MessageLogging(EventId = 8151, Level = LogLevel.Information, Message = "Execution record created for pipeline '{pipelineName}'")]
    public static partial IGenericMessage ExecutionRecordCreated(ILogger logger, string pipelineName);

    [MessageLogging(EventId = 8152, Level = LogLevel.Error, Message = "Failed to create execution record for pipeline '{pipelineName}': {message}")]
    public static partial IGenericMessage ExecutionRecordCreateFailed(ILogger logger, string pipelineName, string message);

    [MessageLogging(EventId = 8153, Level = LogLevel.Information, Message = "Updating execution record '{executionId}' with status '{status}'")]
    public static partial IGenericMessage UpdatingExecutionRecord(ILogger logger, Guid executionId, string status);

    [MessageLogging(EventId = 8154, Level = LogLevel.Information, Message = "Execution record '{executionId}' updated successfully")]
    public static partial IGenericMessage ExecutionRecordUpdated(ILogger logger, Guid executionId);

    [MessageLogging(EventId = 8155, Level = LogLevel.Error, Message = "Failed to update execution record '{executionId}': {message}")]
    public static partial IGenericMessage ExecutionRecordUpdateFailed(ILogger logger, Guid executionId, string message);

    [MessageLogging(EventId = 8156, Level = LogLevel.Information, Message = "Fetching execution history for pipeline '{pipelineName}'")]
    public static partial IGenericMessage FetchingExecutionHistory(ILogger logger, string pipelineName);

    [MessageLogging(EventId = 8157, Level = LogLevel.Information, Message = "Fetching execution record '{executionId}'")]
    public static partial IGenericMessage FetchingExecutionRecord(ILogger logger, Guid executionId);

    [MessageLogging(EventId = 8158, Level = LogLevel.Warning, Message = "Execution record '{executionId}' not found")]
    public static partial IGenericMessage ExecutionRecordNotFound(ILogger logger, Guid executionId);

    [MessageLogging(EventId = 8159, Level = LogLevel.Error, Message = "Failed to fetch execution history: {message}")]
    public static partial IGenericMessage ExecutionHistoryFetchFailed(ILogger logger, string message);

    // ═══════════════════════════════════════════════════════════════════════════
    // Create operations (8200-8209)
    // ═══════════════════════════════════════════════════════════════════════════
    [MessageLogging(EventId = 8200, Level = LogLevel.Information, Message = "Creating pipeline '{name}' of type '{pipelineType}'")]
    public static partial IGenericMessage CreatingPipeline(ILogger logger, string name, string pipelineType);

    [MessageLogging(EventId = 8201, Level = LogLevel.Information, Message = "Pipeline '{name}' created successfully")]
    public static partial IGenericMessage PipelineCreated(ILogger logger, string name);

    [MessageLogging(EventId = 8202, Level = LogLevel.Error, Message = "Failed to create pipeline '{name}': {message}")]
    public static partial IGenericMessage PipelineCreateFailed(ILogger logger, string name, string message);

    [MessageLogging(EventId = 8203, Level = LogLevel.Warning, Message = "Pipeline '{name}' already exists")]
    public static partial IGenericMessage PipelineAlreadyExists(ILogger logger, string name);

    // Read operations (8210-8219)
    [MessageLogging(EventId = 8210, Level = LogLevel.Trace, Message = "Fetching pipeline '{name}'")]
    public static partial IGenericMessage FetchingPipeline(ILogger logger, string name);

    [MessageLogging(EventId = 8211, Level = LogLevel.Information, Message = "Pipeline '{name}' retrieved")]
    public static partial IGenericMessage PipelineRetrieved(ILogger logger, string name);

    [MessageLogging(EventId = 8212, Level = LogLevel.Warning, Message = "Pipeline '{name}' not found")]
    public static partial IGenericMessage PipelineNotFound(ILogger logger, string name);

    [MessageLogging(EventId = 8213, Level = LogLevel.Error, Message = "Failed to fetch pipeline '{name}': {message}")]
    public static partial IGenericMessage PipelineFetchFailed(ILogger logger, string name, string message);

    // Update operations (8220-8229)
    [MessageLogging(EventId = 8220, Level = LogLevel.Information, Message = "Updating pipeline '{name}'")]
    public static partial IGenericMessage UpdatingPipeline(ILogger logger, string name);

    [MessageLogging(EventId = 8221, Level = LogLevel.Information, Message = "Pipeline '{name}' updated successfully")]
    public static partial IGenericMessage PipelineUpdated(ILogger logger, string name);

    // Why: message is nullable — a failed result's CurrentMessage can be absent; the template
    // renders the absence blank rather than the caller fabricating a placeholder value.
    [MessageLogging(EventId = 8222, Level = LogLevel.Error, Message = "Failed to update pipeline '{name}': {message}")]
    public static partial IGenericMessage PipelineUpdateFailed(ILogger logger, string name, string? message);

    // Delete operations (8230-8239)
    [MessageLogging(EventId = 8230, Level = LogLevel.Information, Message = "Deleting pipeline '{name}'")]
    public static partial IGenericMessage DeletingPipeline(ILogger logger, string name);

    [MessageLogging(EventId = 8231, Level = LogLevel.Information, Message = "Pipeline '{name}' deleted successfully")]
    public static partial IGenericMessage PipelineDeleted(ILogger logger, string name);

    [MessageLogging(EventId = 8232, Level = LogLevel.Error, Message = "Failed to delete pipeline '{name}': {message}")]
    public static partial IGenericMessage PipelineDeleteFailed(ILogger logger, string name, string message);

    [MessageLogging(EventId = 8233, Level = LogLevel.Warning, Message = "Cannot delete pipeline '{name}' - has {count} scheduled execution(s)")]
    public static partial IGenericMessage PipelineInUse(ILogger logger, string name, int count);

    // Validation operations (8240-8249)
    [MessageLogging(EventId = 8240, Level = LogLevel.Warning, Message = "Validation failed for pipeline '{name}': {message}")]
    public static partial IGenericMessage PipelineValidationFailed(ILogger logger, string name, string message);

    [MessageLogging(EventId = 8241, Level = LogLevel.Warning, Message = "Unknown pipeline type '{pipelineType}'")]
    public static partial IGenericMessage UnknownPipelineType(ILogger logger, string pipelineType);

    [MessageLogging(EventId = 8242, Level = LogLevel.Warning, Message = "Source connection '{connectionName}' not found for pipeline '{pipelineName}'")]
    public static partial IGenericMessage SourceConnectionNotFound(ILogger logger, string connectionName, string pipelineName);

    [MessageLogging(EventId = 8243, Level = LogLevel.Warning, Message = "Target connection '{connectionName}' not found for pipeline '{pipelineName}'")]
    public static partial IGenericMessage TargetConnectionNotFound(ILogger logger, string connectionName, string pipelineName);
}
