using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceEtl.Endpoints.Nodes.Logging;

/// <summary>
/// MessageLogging for giving a project step its pipelines and the prerequisites between them. Every message is a validation
/// refusal (category 2), so the shared status mapper answers it with a client error.
/// </summary>
[ExcludeFromCodeCoverage]
[MessageLoggingTypeCode("NODES")]
public static partial class NodePipelineLog
{
    /// <summary>A node that cannot hold pipelines was sent pipelines or prerequisites.</summary>
    [MessageLogging(EventId = 21000, Level = LogLevel.Warning,
        Message = "Node '{nodeName}' is a {nodeType} and cannot hold pipelines or prerequisites",
        ClientMessage = "Node '{nodeName}' is a {nodeType} and cannot hold pipelines or prerequisites; only a Step can")]
    public static partial IGenericMessage PipelinesNotAllowed(ILogger logger, string nodeName, string nodeType);

    /// <summary>A pipeline name sent for a node does not name a pipeline.</summary>
    [MessageLogging(EventId = 21001, Level = LogLevel.Warning,
        Message = "Pipeline '{pipelineName}' named for node '{nodeName}' does not exist",
        ClientMessage = "Pipeline '{pipelineName}' does not exist")]
    public static partial IGenericMessage PipelineNotFound(ILogger logger, string nodeName, string pipelineName);

    /// <summary>The same pipeline was named twice for one node.</summary>
    [MessageLogging(EventId = 21002, Level = LogLevel.Warning,
        Message = "Pipeline '{pipelineName}' is named more than once for node '{nodeName}'",
        ClientMessage = "Pipeline '{pipelineName}' is named more than once; a step holds each pipeline once")]
    public static partial IGenericMessage PipelineRepeated(ILogger logger, string nodeName, string pipelineName);
}
