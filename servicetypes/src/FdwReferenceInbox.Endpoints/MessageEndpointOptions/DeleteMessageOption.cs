using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceMessages.Endpoints.MessageEndpointOptions;

/// <summary>The DeleteMessage endpoint.</summary>
[WebMcpTool("delete_message", "Delete message.")]
[TypeOption(typeof(MessageEndpoints), "DeleteMessage")]
public class DeleteMessageOption : MessageEndpointBase<DeleteMessageEndpoint>
{
}
