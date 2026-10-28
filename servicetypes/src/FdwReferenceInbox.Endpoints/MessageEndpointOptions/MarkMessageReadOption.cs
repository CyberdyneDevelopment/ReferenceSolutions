using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceMessages.Endpoints.MessageEndpointOptions;

/// <summary>The MarkMessageRead endpoint.</summary>
[WebMcpTool("mark_message_read", "Mark message read.")]
[TypeOption(typeof(MessageEndpoints), "MarkMessageRead")]
public class MarkMessageReadOption : MessageEndpointBase<MarkMessageReadEndpoint>
{
}
