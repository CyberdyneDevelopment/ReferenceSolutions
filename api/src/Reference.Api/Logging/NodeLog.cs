using System.Diagnostics.CodeAnalysis;
using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Logging;

/// <summary>
/// MessageLogging definitions for orchestration node endpoints.
/// EventId range: 9230-9259
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class NodeLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // List operations (9230-9234)
    // ═══════════════════════════════════════════════════════════════════════════

    [MessageLogging(EventId = 9230, Level = LogLevel.Trace, Message = "Listing root orchestration nodes")]
    public static partial IGenericMessage ListingRootNodes(ILogger logger);

    [MessageLogging(EventId = 9231, Level = LogLevel.Information, Message = "Listed {count} root nodes")]
    public static partial IGenericMessage RootNodesListed(ILogger logger, int count);

    [MessageLogging(EventId = 9232, Level = LogLevel.Error, Message = "Failed to list root nodes: {message}")]
    public static partial IGenericMessage RootNodesListFailed(ILogger logger, string message);

    // ═══════════════════════════════════════════════════════════════════════════
    // Get operations (9235-9239)
    // ═══════════════════════════════════════════════════════════════════════════

    [MessageLogging(EventId = 9235, Level = LogLevel.Trace, Message = "Getting node '{nodeId}'")]
    public static partial IGenericMessage GettingNode(ILogger logger, Guid nodeId);

    [MessageLogging(EventId = 9236, Level = LogLevel.Information, Message = "Node '{nodeId}' retrieved")]
    public static partial IGenericMessage NodeRetrieved(ILogger logger, Guid nodeId);

    [MessageLogging(EventId = 9237, Level = LogLevel.Warning, Message = "Node '{nodeId}' not found")]
    public static partial IGenericMessage NodeNotFound(ILogger logger, Guid nodeId);

    [MessageLogging(EventId = 9238, Level = LogLevel.Error, Message = "Failed to get node '{nodeId}': {message}")]
    public static partial IGenericMessage NodeGetFailed(ILogger logger, Guid nodeId, string message);

    [MessageLogging(EventId = 9239, Level = LogLevel.Trace, Message = "Getting node '{nodeId}' to depth {depth}")]
    public static partial IGenericMessage GettingNodeDeep(ILogger logger, Guid nodeId, int depth);

    // ═══════════════════════════════════════════════════════════════════════════
    // Create operations (9240-9244)
    // ═══════════════════════════════════════════════════════════════════════════

    [MessageLogging(EventId = 9240, Level = LogLevel.Debug, Message = "Creating node '{name}'")]
    public static partial IGenericMessage CreatingNode(ILogger logger, string name);

    [MessageLogging(EventId = 9241, Level = LogLevel.Information, Message = "Node '{name}' created with ID '{nodeId}'")]
    public static partial IGenericMessage NodeCreated(ILogger logger, string name, Guid nodeId);

    [MessageLogging(EventId = 9242, Level = LogLevel.Error, Message = "Failed to create node '{name}': {message}")]
    public static partial IGenericMessage NodeCreateFailed(ILogger logger, string name, string message);

    // ═══════════════════════════════════════════════════════════════════════════
    // Update operations (9245-9249)
    // ═══════════════════════════════════════════════════════════════════════════

    [MessageLogging(EventId = 9245, Level = LogLevel.Debug, Message = "Updating node '{nodeId}'")]
    public static partial IGenericMessage UpdatingNode(ILogger logger, Guid nodeId);

    [MessageLogging(EventId = 9246, Level = LogLevel.Information, Message = "Node '{nodeId}' updated")]
    public static partial IGenericMessage NodeUpdated(ILogger logger, Guid nodeId);

    [MessageLogging(EventId = 9247, Level = LogLevel.Warning, Message = "Node '{nodeId}' not found for update")]
    public static partial IGenericMessage NodeNotFoundForUpdate(ILogger logger, Guid nodeId);

    [MessageLogging(EventId = 9248, Level = LogLevel.Error, Message = "Failed to update node '{nodeId}': {message}")]
    public static partial IGenericMessage NodeUpdateFailed(ILogger logger, Guid nodeId, string message);

    // ═══════════════════════════════════════════════════════════════════════════
    // Delete operations (9250-9254)
    // ═══════════════════════════════════════════════════════════════════════════

    [MessageLogging(EventId = 9250, Level = LogLevel.Debug, Message = "Deleting node '{nodeId}'")]
    public static partial IGenericMessage DeletingNode(ILogger logger, Guid nodeId);

    [MessageLogging(EventId = 9251, Level = LogLevel.Information, Message = "Node '{nodeId}' deleted")]
    public static partial IGenericMessage NodeDeleted(ILogger logger, Guid nodeId);

    [MessageLogging(EventId = 9252, Level = LogLevel.Warning, Message = "Node '{nodeId}' not found for delete")]
    public static partial IGenericMessage NodeNotFoundForDelete(ILogger logger, Guid nodeId);

    [MessageLogging(EventId = 9253, Level = LogLevel.Error, Message = "Failed to delete node '{nodeId}': {message}")]
    public static partial IGenericMessage NodeDeleteFailed(ILogger logger, Guid nodeId, string message);
}
