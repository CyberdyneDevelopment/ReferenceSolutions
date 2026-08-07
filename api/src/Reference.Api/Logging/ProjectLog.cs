using System.Diagnostics.CodeAnalysis;
using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Logging;

/// <summary>
/// MessageLogging definitions for ETL Project orchestration node endpoints.
/// EventId range: 9200-9229
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class ProjectLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // List operations (9200-9204)
    // ═══════════════════════════════════════════════════════════════════════════

    [MessageLogging(EventId = 9200, Level = LogLevel.Trace, Message = "Listing projects")]
    public static partial IGenericMessage ListingProjects(ILogger logger);

    [MessageLogging(EventId = 9201, Level = LogLevel.Information, Message = "Listed {count} projects")]
    public static partial IGenericMessage ProjectsListed(ILogger logger, int count);

    [MessageLogging(EventId = 9202, Level = LogLevel.Error, Message = "Failed to list projects: {message}")]
    public static partial IGenericMessage ProjectsListFailed(ILogger logger, string message);

    [MessageLogging(EventId = 9203, Level = LogLevel.Error, Message = "Project tree mapping failed — node '{nodeId}' has child with unrecognized NodeTypeId {nodeTypeId} (childId='{childId}')")]
    public static partial IGenericMessage ProjectUnknownChildNodeType(ILogger logger, Guid nodeId, int nodeTypeId, Guid childId);

    // ═══════════════════════════════════════════════════════════════════════════
    // Get operations (9205-9209)
    // ═══════════════════════════════════════════════════════════════════════════

    [MessageLogging(EventId = 9205, Level = LogLevel.Trace, Message = "Getting project '{projectId}'")]
    public static partial IGenericMessage GettingProject(ILogger logger, Guid projectId);

    [MessageLogging(EventId = 9206, Level = LogLevel.Information, Message = "Project '{projectId}' retrieved")]
    public static partial IGenericMessage ProjectRetrieved(ILogger logger, Guid projectId);

    [MessageLogging(EventId = 9207, Level = LogLevel.Warning, Message = "Project '{projectId}' not found")]
    public static partial IGenericMessage ProjectNotFound(ILogger logger, Guid projectId);

    [MessageLogging(EventId = 9208, Level = LogLevel.Error, Message = "Failed to get project '{projectId}': {message}")]
    public static partial IGenericMessage ProjectGetFailed(ILogger logger, Guid projectId, string message);

    // ═══════════════════════════════════════════════════════════════════════════
    // Create operations (9210-9214)
    // ═══════════════════════════════════════════════════════════════════════════

    [MessageLogging(EventId = 9210, Level = LogLevel.Debug, Message = "Creating project '{name}'")]
    public static partial IGenericMessage CreatingProject(ILogger logger, string name);

    [MessageLogging(EventId = 9211, Level = LogLevel.Information, Message = "Project '{name}' created with ID '{projectId}'")]
    public static partial IGenericMessage ProjectCreated(ILogger logger, string name, Guid projectId);

    [MessageLogging(EventId = 9212, Level = LogLevel.Error, Message = "Failed to create project '{name}': {message}")]
    public static partial IGenericMessage ProjectCreateFailed(ILogger logger, string name, string message);

    // ═══════════════════════════════════════════════════════════════════════════
    // Update operations (9215-9219)
    // ═══════════════════════════════════════════════════════════════════════════

    [MessageLogging(EventId = 9215, Level = LogLevel.Debug, Message = "Updating project '{projectId}'")]
    public static partial IGenericMessage UpdatingProject(ILogger logger, Guid projectId);

    [MessageLogging(EventId = 9216, Level = LogLevel.Information, Message = "Project '{projectId}' updated")]
    public static partial IGenericMessage ProjectUpdated(ILogger logger, Guid projectId);

    [MessageLogging(EventId = 9217, Level = LogLevel.Warning, Message = "Project '{projectId}' not found for update")]
    public static partial IGenericMessage ProjectNotFoundForUpdate(ILogger logger, Guid projectId);

    [MessageLogging(EventId = 9218, Level = LogLevel.Error, Message = "Failed to update project '{projectId}': {message}")]
    public static partial IGenericMessage ProjectUpdateFailed(ILogger logger, Guid projectId, string message);

    // ═══════════════════════════════════════════════════════════════════════════
    // Delete operations (9220-9224)
    // ═══════════════════════════════════════════════════════════════════════════

    [MessageLogging(EventId = 9220, Level = LogLevel.Debug, Message = "Deleting project '{projectId}'")]
    public static partial IGenericMessage DeletingProject(ILogger logger, Guid projectId);

    [MessageLogging(EventId = 9221, Level = LogLevel.Information, Message = "Project '{projectId}' deleted")]
    public static partial IGenericMessage ProjectDeleted(ILogger logger, Guid projectId);

    [MessageLogging(EventId = 9222, Level = LogLevel.Warning, Message = "Project '{projectId}' not found for delete")]
    public static partial IGenericMessage ProjectNotFoundForDelete(ILogger logger, Guid projectId);

    [MessageLogging(EventId = 9223, Level = LogLevel.Error, Message = "Failed to delete project '{projectId}': {message}")]
    public static partial IGenericMessage ProjectDeleteFailed(ILogger logger, Guid projectId, string message);

    // ═══════════════════════════════════════════════════════════════════════════
    // Trigger operations (9225-9229)
    // ═══════════════════════════════════════════════════════════════════════════

    [MessageLogging(EventId = 9225, Level = LogLevel.Information, Message = "Triggering ETL node type '{type}' for node '{nodeId}'")]
    public static partial IGenericMessage TriggeringEtlNode(ILogger logger, string type, Guid nodeId);

    [MessageLogging(EventId = 9226, Level = LogLevel.Information, Message = "ETL node '{nodeId}' queued with execution ID '{executionId}'")]
    public static partial IGenericMessage EtlNodeQueued(ILogger logger, Guid nodeId, Guid executionId);

    [MessageLogging(EventId = 9227, Level = LogLevel.Warning, Message = "ETL trigger queue full — node '{nodeId}' rejected")]
    public static partial IGenericMessage EtlTriggerQueueFull(ILogger logger, Guid nodeId);

    [MessageLogging(EventId = 9228, Level = LogLevel.Warning, Message = "ETL trigger: node not found (type='{type}', id='{id}', name='{name}')")]
    public static partial IGenericMessage EtlTriggerNodeNotFound(ILogger logger, string type, string id, string name);

    [MessageLogging(EventId = 9229, Level = LogLevel.Error, Message = "ETL trigger failed for type '{type}': {message}")]
    public static partial IGenericMessage EtlTriggerFailed(ILogger logger, string type, string message);
}
