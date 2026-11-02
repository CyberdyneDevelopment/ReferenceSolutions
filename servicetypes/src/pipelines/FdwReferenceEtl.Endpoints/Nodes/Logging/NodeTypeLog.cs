using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceEtl.Endpoints.Nodes.Logging;

/// <summary>
/// MessageLogging for naming the type of a node being created. Every message is a validation refusal (category 2), so the shared
/// status mapper answers it with a client error.
/// </summary>
[ExcludeFromCodeCoverage]
[MessageLoggingTypeCode("NODES")]
public static partial class NodeTypeLog
{
    /// <summary>A node was sent with neither a node type name nor a node type id.</summary>
    [MessageLogging(EventId = 21010, Level = LogLevel.Warning,
        Message = "Node '{nodeName}' does not say what it is: send nodeType (one of {knownTypes})",
        ClientMessage = "Say what the node is: send nodeType, one of {knownTypes}")]
    public static partial IGenericMessage NodeTypeNotStated(ILogger logger, string nodeName, string knownTypes);

    /// <summary>The node type name sent is not a node type.</summary>
    [MessageLogging(EventId = 21011, Level = LogLevel.Warning,
        Message = "Node '{nodeName}' names node type '{nodeType}', which does not exist; the node types are {knownTypes}",
        ClientMessage = "nodeType '{nodeType}' does not exist; send one of {knownTypes}")]
    public static partial IGenericMessage NodeTypeUnknown(ILogger logger, string nodeName, string nodeType, string knownTypes);

    /// <summary>The node type name and the node type id sent name different types.</summary>
    [MessageLogging(EventId = 21012, Level = LogLevel.Warning,
        Message = "Node '{nodeName}' sends nodeType '{nodeType}' and nodeTypeId {nodeTypeId}, which are different types",
        ClientMessage = "nodeType '{nodeType}' and nodeTypeId {nodeTypeId} are different types; send one of them, or two that agree")]
    public static partial IGenericMessage NodeTypeDisagrees(ILogger logger, string nodeName, string nodeType, int nodeTypeId);
}
