using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Reference.Etl.Server.Logging;

/// <summary>
/// MessageLogging for ETL Server operations.
/// EventId range: 10100-10349
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class EtlServerLog
{
    [MessageLogging(
        EventId = 10100,
        Level = LogLevel.Information,
        Message = "Job '{jobId}' triggered by {triggerSource}")]
    public static partial IGenericMessage JobTriggered(ILogger logger, string jobId, string triggerSource);

    [MessageLogging(
        EventId = 10101,
        Level = LogLevel.Information,
        Message = "Job '{jobId}' started, pipeline '{pipelineName}'")]
    public static partial IGenericMessage JobStarted(ILogger logger, string jobId, string pipelineName);

    [MessageLogging(
        EventId = 10102,
        Level = LogLevel.Information,
        Message = "Job '{jobId}' completed successfully in {duration}ms")]
    public static partial IGenericMessage JobCompleted(ILogger logger, string jobId, long duration);

    [MessageLogging(
        EventId = 10103,
        Level = LogLevel.Error,
        Message = "Job '{jobId}' failed: {error}")]
    public static partial IGenericMessage JobFailed(ILogger logger, Exception ex, string jobId, string error);

    [MessageLogging(
        EventId = 10110,
        Level = LogLevel.Debug,
        Message = "Pipeline '{pipelineName}' execution started")]
    public static partial IGenericMessage PipelineExecutionStarted(ILogger logger, string pipelineName);

    [MessageLogging(
        EventId = 10111,
        Level = LogLevel.Debug,
        Message = "Pipeline '{pipelineName}' step '{stepName}' completed")]
    public static partial IGenericMessage PipelineStepCompleted(ILogger logger, string pipelineName, string stepName);

    [MessageLogging(
        EventId = 10112,
        Level = LogLevel.Information,
        Message = "Pipeline '{pipelineName}' execution completed, {rowsProcessed} rows processed")]
    public static partial IGenericMessage PipelineExecutionCompleted(ILogger logger, string pipelineName, int rowsProcessed);

    [MessageLogging(
        EventId = 10120,
        Level = LogLevel.Debug,
        Message = "Trigger source '{sourceName}' registered")]
    public static partial IGenericMessage TriggerSourceRegistered(ILogger logger, string sourceName);

    [MessageLogging(
        EventId = 10130,
        Level = LogLevel.Warning,
        Message = "Job '{jobId}' not found")]
    public static partial IGenericMessage JobNotFound(ILogger logger, string jobId);

    // API Client methods (10200-10209)

    [MessageLogging(
        EventId = 10200,
        Level = LogLevel.Warning,
        Message = "API call to '{endpoint}' failed with status {statusCode}")]
    public static partial IGenericMessage ApiCallFailed(ILogger logger, string endpoint, int statusCode);

    [MessageLogging(
        EventId = 10201,
        Level = LogLevel.Warning,
        Message = "API response from '{endpoint}' was empty")]
    public static partial IGenericMessage ApiResponseEmpty(ILogger logger, string endpoint);

    [MessageLogging(
        EventId = 10202,
        Level = LogLevel.Error,
        Message = "API call to '{endpoint}' threw exception: {error}")]
    public static partial IGenericMessage ApiCallException(ILogger logger, string endpoint, string error);

    // Pipeline phases (10210-10219)

    [MessageLogging(
        EventId = 10210,
        Level = LogLevel.Information,
        Message = "Pipeline '{pipelineName}' started")]
    public static partial IGenericMessage PipelineStarted(ILogger logger, string pipelineName);

    [MessageLogging(
        EventId = 10211,
        Level = LogLevel.Debug,
        Message = "Extracting from API endpoint: {endpoint}")]
    public static partial IGenericMessage ExtractingFromApi(ILogger logger, string endpoint);

    [MessageLogging(
        EventId = 10212,
        Level = LogLevel.Information,
        Message = "Extract completed: {count} records")]
    public static partial IGenericMessage ExtractCompleted(ILogger logger, int count);

    [MessageLogging(
        EventId = 10213,
        Level = LogLevel.Error,
        Message = "Extract failed for pipeline '{pipelineName}'")]
    public static partial IGenericMessage ExtractFailed(ILogger logger, string pipelineName);

    [MessageLogging(
        EventId = 10214,
        Level = LogLevel.Information,
        Message = "Transform completed: {transformed} records, {filtered} filtered")]
    public static partial IGenericMessage TransformCompleted(ILogger logger, int transformed, int filtered);

    [MessageLogging(
        EventId = 10215,
        Level = LogLevel.Information,
        Message = "Pipeline '{pipelineName}' completed: {records} records in {seconds:F2}s")]
    public static partial IGenericMessage PipelineCompleted(ILogger logger, string pipelineName, int records, double seconds);

    [MessageLogging(
        EventId = 10216,
        Level = LogLevel.Warning,
        Message = "Pipeline '{pipelineName}' was cancelled")]
    public static partial IGenericMessage PipelineCancelled(ILogger logger, string pipelineName);

    [MessageLogging(
        EventId = 10217,
        Level = LogLevel.Error,
        Message = "Pipeline '{pipelineName}' failed: {error}")]
    public static partial IGenericMessage PipelineFailed(ILogger logger, string pipelineName, string error);

    // Repository operations (10220-10239)

    [MessageLogging(
        EventId = 10220,
        Level = LogLevel.Debug,
        Message = "Creating execution record for pipeline '{pipelineName}'")]
    public static partial IGenericMessage ExecutionRecordCreating(ILogger logger, string pipelineName);

    [MessageLogging(
        EventId = 10221,
        Level = LogLevel.Information,
        Message = "Execution record '{executionId}' created for pipeline '{pipelineName}'")]
    public static partial IGenericMessage ExecutionRecordCreated(ILogger logger, Guid executionId, string pipelineName);

    [MessageLogging(
        EventId = 10222,
        Level = LogLevel.Error,
        Message = "Failed to create execution record for pipeline '{pipelineName}': {error}")]
    public static partial IGenericMessage ExecutionRecordCreateFailed(ILogger logger, Exception ex, string pipelineName, string error);

    [MessageLogging(
        EventId = 10223,
        Level = LogLevel.Debug,
        Message = "Completing execution record '{executionId}' with status '{status}'")]
    public static partial IGenericMessage ExecutionRecordCompleting(ILogger logger, Guid executionId, string status);

    [MessageLogging(
        EventId = 10224,
        Level = LogLevel.Information,
        Message = "Execution record '{executionId}' completed with status '{status}'")]
    public static partial IGenericMessage ExecutionRecordCompleted(ILogger logger, Guid executionId, string status);

    [MessageLogging(
        EventId = 10225,
        Level = LogLevel.Error,
        Message = "Failed to complete execution record '{executionId}': {error}")]
    public static partial IGenericMessage ExecutionRecordCompleteFailed(ILogger logger, Exception ex, Guid executionId, string error);

    [MessageLogging(
        EventId = 10226,
        Level = LogLevel.Warning,
        Message = "Execution record '{executionId}' not found")]
    public static partial IGenericMessage ExecutionRecordNotFound(ILogger logger, Guid executionId);

    [MessageLogging(
        EventId = 10227,
        Level = LogLevel.Error,
        Message = "Failed to get execution record '{executionId}': {error}")]
    public static partial IGenericMessage ExecutionRecordGetFailed(ILogger logger, Exception ex, Guid executionId, string error);

    [MessageLogging(
        EventId = 10228,
        Level = LogLevel.Warning,
        Message = "ControlDb connection not available")]
    public static partial IGenericMessage ControlDbNotAvailable(ILogger logger);

    // Background execution (10240-10249)

    [MessageLogging(
        EventId = 10240,
        Level = LogLevel.Information,
        Message = "Pipeline '{pipelineName}' execution queued with ID '{executionId}'")]
    public static partial IGenericMessage PipelineExecutionQueued(ILogger logger, string pipelineName, Guid executionId);

    [MessageLogging(
        EventId = 10241,
        Level = LogLevel.Information,
        Message = "Pipeline '{pipelineName}' execution '{executionId}' succeeded in {durationMs}ms")]
    public static partial IGenericMessage PipelineExecutionSucceeded(ILogger logger, string pipelineName, Guid executionId, long durationMs);

    [MessageLogging(
        EventId = 10242,
        Level = LogLevel.Error,
        Message = "Pipeline '{pipelineName}' execution '{executionId}' failed: {error}")]
    public static partial IGenericMessage PipelineExecutionFailed(ILogger logger, Exception ex, string pipelineName, Guid executionId, string error);

    // Endpoint trace events (10250-10259)

    [MessageLogging(
        EventId = 10250,
        Level = LogLevel.Trace,
        Message = "Trigger job request received for pipeline '{pipelineName}'")]
    public static partial IGenericMessage TriggerJobRequestReceived(ILogger logger, string pipelineName);

    [MessageLogging(
        EventId = 10251,
        Level = LogLevel.Trace,
        Message = "Job status request received for execution '{executionId}'")]
    public static partial IGenericMessage JobStatusRequestReceived(ILogger logger, Guid executionId);

    [MessageLogging(
        EventId = 10260,
        Level = LogLevel.Error,
        Message = "Operation failed with no error message")]
    public static partial IGenericMessage OperationFailedNoMessage(ILogger logger);

    // Unified trigger endpoint (10261-10269)

    [MessageLogging(
        EventId = 10261,
        Level = LogLevel.Trace,
        Message = "Unified trigger request received: type='{type}' name='{name}'")]
    public static partial IGenericMessage UnifiedTriggerRequestReceived(ILogger logger, string type, string name);

    [MessageLogging(
        EventId = 10262,
        Level = LogLevel.Information,
        Message = "Project '{projectName}' queued for execution, executionId='{executionId}'")]
    public static partial IGenericMessage ProjectExecutionQueued(ILogger logger, string projectName, Guid executionId);

    [MessageLogging(
        EventId = 10263,
        Level = LogLevel.Information,
        Message = "Execution '{executionId}' created in AwaitingApproval state for '{name}'")]
    public static partial IGenericMessage ExecutionAwaitingApproval(ILogger logger, Guid executionId, string name);

    [MessageLogging(
        EventId = 10264,
        Level = LogLevel.Warning,
        Message = "Unified trigger queue full for type='{type}' name='{name}'")]
    public static partial IGenericMessage UnifiedTriggerQueueFull(ILogger logger, string type, string name);

    [MessageLogging(
        EventId = 10265,
        Level = LogLevel.Warning,
        Message = "Project '{projectName}' not found for trigger")]
    public static partial IGenericMessage ProjectNotFoundForTrigger(ILogger logger, string projectName);

    [MessageLogging(
        EventId = 10266,
        Level = LogLevel.Warning,
        Message = "Unknown trigger type '{type}'; must be a registered ExecutionItemType name")]
    public static partial IGenericMessage UnknownTriggerType(ILogger logger, string type);

    // Execution control endpoints (10270-10279)

    [MessageLogging(
        EventId = 10270,
        Level = LogLevel.Trace,
        Message = "Cancel execution request received for '{executionId}'")]
    public static partial IGenericMessage CancelExecutionRequestReceived(ILogger logger, Guid executionId);

    [MessageLogging(
        EventId = 10271,
        Level = LogLevel.Information,
        Message = "Execution '{executionId}' cancelled")]
    public static partial IGenericMessage ExecutionCancelled(ILogger logger, Guid executionId);

    [MessageLogging(
        EventId = 10272,
        Level = LogLevel.Trace,
        Message = "Approve execution request received for '{executionId}'")]
    public static partial IGenericMessage ApproveExecutionRequestReceived(ILogger logger, Guid executionId);

    [MessageLogging(
        EventId = 10273,
        Level = LogLevel.Information,
        Message = "Execution '{executionId}' approved and enqueued")]
    public static partial IGenericMessage ExecutionApprovedAndEnqueued(ILogger logger, Guid executionId);

    [MessageLogging(
        EventId = 10274,
        Level = LogLevel.Warning,
        Message = "Execution '{executionId}' not in AwaitingApproval state, cannot approve")]
    public static partial IGenericMessage ExecutionNotAwaitingApproval(ILogger logger, Guid executionId);

    [MessageLogging(
        EventId = 10275,
        Level = LogLevel.Trace,
        Message = "Execution status request received for '{executionId}'")]
    public static partial IGenericMessage ExecutionStatusRequestReceived(ILogger logger, Guid executionId);

    // Project CRUD (10280-10289)

    [MessageLogging(
        EventId = 10280,
        Level = LogLevel.Trace,
        Message = "Create project request received: '{name}'")]
    public static partial IGenericMessage CreateProjectRequestReceived(ILogger logger, string name);

    [MessageLogging(
        EventId = 10281,
        Level = LogLevel.Information,
        Message = "Project '{name}' created with id='{id}'")]
    public static partial IGenericMessage ProjectCreated(ILogger logger, string name, Guid id);

    [MessageLogging(
        EventId = 10282,
        Level = LogLevel.Trace,
        Message = "Update project request received for '{id}'")]
    public static partial IGenericMessage UpdateProjectRequestReceived(ILogger logger, Guid id);

    [MessageLogging(
        EventId = 10283,
        Level = LogLevel.Information,
        Message = "Project '{id}' updated")]
    public static partial IGenericMessage ProjectUpdated(ILogger logger, Guid id);

    [MessageLogging(
        EventId = 10284,
        Level = LogLevel.Trace,
        Message = "Delete project request received for '{id}'")]
    public static partial IGenericMessage DeleteProjectRequestReceived(ILogger logger, Guid id);

    [MessageLogging(
        EventId = 10285,
        Level = LogLevel.Information,
        Message = "Project '{id}' deleted")]
    public static partial IGenericMessage ProjectDeleted(ILogger logger, Guid id);

    [MessageLogging(
        EventId = 10286,
        Level = LogLevel.Warning,
        Message = "Project '{id}' not found")]
    public static partial IGenericMessage ProjectNotFound(ILogger logger, Guid id);

    [MessageLogging(
        EventId = 10287,
        Level = LogLevel.Error,
        Message = "Project save failed: {error}")]
    public static partial IGenericMessage ProjectSaveFailed(ILogger logger, Exception ex, string error);

    // Stage CRUD (10290-10299)

    [MessageLogging(
        EventId = 10290,
        Level = LogLevel.Trace,
        Message = "Create stage request received: '{name}' under project '{projectId}'")]
    public static partial IGenericMessage CreateStageRequestReceived(ILogger logger, string name, Guid projectId);

    [MessageLogging(
        EventId = 10291,
        Level = LogLevel.Information,
        Message = "Stage '{name}' created with id='{id}'")]
    public static partial IGenericMessage StageCreated(ILogger logger, string name, Guid id);

    [MessageLogging(
        EventId = 10292,
        Level = LogLevel.Warning,
        Message = "Stage '{id}' not found")]
    public static partial IGenericMessage StageNotFound(ILogger logger, Guid id);

    [MessageLogging(
        EventId = 10293,
        Level = LogLevel.Information,
        Message = "Stage '{id}' updated")]
    public static partial IGenericMessage StageUpdated(ILogger logger, Guid id);

    [MessageLogging(
        EventId = 10294,
        Level = LogLevel.Information,
        Message = "Stage '{id}' deleted")]
    public static partial IGenericMessage StageDeleted(ILogger logger, Guid id);

    [MessageLogging(
        EventId = 10295,
        Level = LogLevel.Error,
        Message = "Stage save failed: {error}")]
    public static partial IGenericMessage StageSaveFailed(ILogger logger, Exception ex, string error);

    // Step CRUD (10300-10319)

    [MessageLogging(
        EventId = 10300,
        Level = LogLevel.Trace,
        Message = "Create step request received: '{name}' under stage '{stageId}'")]
    public static partial IGenericMessage CreateStepRequestReceived(ILogger logger, string name, Guid stageId);

    [MessageLogging(
        EventId = 10301,
        Level = LogLevel.Information,
        Message = "Step '{name}' created with id='{id}'")]
    public static partial IGenericMessage StepCreated(ILogger logger, string name, Guid id);

    [MessageLogging(
        EventId = 10302,
        Level = LogLevel.Warning,
        Message = "Step '{id}' not found")]
    public static partial IGenericMessage StepNotFound(ILogger logger, Guid id);

    [MessageLogging(
        EventId = 10303,
        Level = LogLevel.Information,
        Message = "Step '{id}' updated")]
    public static partial IGenericMessage StepUpdated(ILogger logger, Guid id);

    [MessageLogging(
        EventId = 10304,
        Level = LogLevel.Information,
        Message = "Step '{id}' deleted")]
    public static partial IGenericMessage StepDeleted(ILogger logger, Guid id);

    [MessageLogging(
        EventId = 10305,
        Level = LogLevel.Error,
        Message = "Step save failed: {error}")]
    public static partial IGenericMessage StepSaveFailed(ILogger logger, Exception ex, string error);

    [MessageLogging(
        EventId = 10306,
        Level = LogLevel.Information,
        Message = "Pipeline membership added to step '{stepId}', pipelineId='{pipelineId}'")]
    public static partial IGenericMessage StepPipelineAdded(ILogger logger, Guid stepId, Guid pipelineId);

    [MessageLogging(
        EventId = 10307,
        Level = LogLevel.Information,
        Message = "Pipeline '{pipelineId}' removed from step '{stepId}'")]
    public static partial IGenericMessage StepPipelineRemoved(ILogger logger, Guid stepId, Guid pipelineId);

    [MessageLogging(
        EventId = 10308,
        Level = LogLevel.Information,
        Message = "Prerequisite added to step '{stepId}'")]
    public static partial IGenericMessage PrerequisiteAdded(ILogger logger, Guid stepId);

    [MessageLogging(
        EventId = 10309,
        Level = LogLevel.Information,
        Message = "Prerequisite '{prerequisiteId}' removed from step '{stepId}'")]
    public static partial IGenericMessage PrerequisiteRemoved(ILogger logger, Guid stepId, Guid prerequisiteId);

    // Inspect endpoints (10330-10339)

    [MessageLogging(
        EventId = 10330,
        Level = LogLevel.Trace,
        Message = "Inspect task request received for execution='{executionId}', task='{taskId}'")]
    public static partial IGenericMessage InspectTaskRequestReceived(ILogger logger, Guid executionId, Guid taskId);

    [MessageLogging(
        EventId = 10331,
        Level = LogLevel.Trace,
        Message = "Inspect edge request received for execution='{executionId}', edge='{sourceTaskId}->{targetTaskId}'")]
    public static partial IGenericMessage InspectEdgeRequestReceived(ILogger logger, Guid executionId, Guid sourceTaskId, Guid targetTaskId);

    // Lineage expand (10340-10349)

    [MessageLogging(
        EventId = 10340,
        Level = LogLevel.Trace,
        Message = "Expand lineage node request received: type='{nodeType}', id='{nodeId}'")]
    public static partial IGenericMessage ExpandLineageNodeRequestReceived(ILogger logger, string nodeType, string nodeId);

    [MessageLogging(
        EventId = 10341,
        Level = LogLevel.Warning,
        Message = "Lineage node not found: type='{nodeType}', id='{nodeId}'")]
    public static partial IGenericMessage LineageNodeNotFound(ILogger logger, string nodeType, string nodeId);

    // OrchestrationNode generic CRUD (10350-10369)

    [MessageLogging(
        EventId = 10350,
        Level = LogLevel.Trace,
        Message = "Create node request received: name='{name}', nodeType='{nodeType}'")]
    public static partial IGenericMessage CreateNodeRequestReceived(ILogger logger, string name, string nodeType);

    [MessageLogging(
        EventId = 10351,
        Level = LogLevel.Information,
        Message = "Node '{name}' created with id='{id}'")]
    public static partial IGenericMessage NodeCreated(ILogger logger, string name, Guid id);

    [MessageLogging(
        EventId = 10352,
        Level = LogLevel.Error,
        Message = "Node save failed: {error}")]
    public static partial IGenericMessage NodeSaveFailed(ILogger logger, Exception ex, string error);

    [MessageLogging(
        EventId = 10353,
        Level = LogLevel.Trace,
        Message = "Get node request received for id='{id}'")]
    public static partial IGenericMessage GetNodeRequestReceived(ILogger logger, Guid id);

    [MessageLogging(
        EventId = 10354,
        Level = LogLevel.Warning,
        Message = "Node '{id}' not found")]
    public static partial IGenericMessage NodeNotFound(ILogger logger, Guid id);

    [MessageLogging(
        EventId = 10355,
        Level = LogLevel.Trace,
        Message = "Update node request received for id='{id}'")]
    public static partial IGenericMessage UpdateNodeRequestReceived(ILogger logger, Guid id);

    [MessageLogging(
        EventId = 10356,
        Level = LogLevel.Information,
        Message = "Node '{id}' updated")]
    public static partial IGenericMessage NodeUpdated(ILogger logger, Guid id);

    [MessageLogging(
        EventId = 10357,
        Level = LogLevel.Trace,
        Message = "Delete node request received for id='{id}'")]
    public static partial IGenericMessage DeleteNodeRequestReceived(ILogger logger, Guid id);

    [MessageLogging(
        EventId = 10358,
        Level = LogLevel.Information,
        Message = "Node '{id}' deleted")]
    public static partial IGenericMessage NodeDeleted(ILogger logger, Guid id);

    [MessageLogging(
        EventId = 10359,
        Level = LogLevel.Warning,
        Message = "Node type '{nodeType}' not found in OrchestrationNodeTypes")]
    public static partial IGenericMessage NodeTypeNotFound(ILogger logger, string nodeType);

    // Non-fatal trigger lifecycle warnings (10360-10361)

    [MessageLogging(
        EventId = 10360,
        Level = LogLevel.Error,
        Message = "ETL metrics insert failed for execution '{executionId}', pipeline '{pipelineName}': {message}")]
    public static partial IGenericMessage EtlMetricsInsertFailed(ILogger logger, Guid executionId, string pipelineName, string? message);

    [MessageLogging(
        EventId = 10361,
        Level = LogLevel.Error,
        Message = "Execution Complete call failed for execution '{executionId}', pipeline '{pipelineName}': {message}")]
    public static partial IGenericMessage ExecutionCompleteFailed(ILogger logger, Guid executionId, string pipelineName, string? message);

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
