using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceMessages.Endpoints.MessageEndpointOptions;

/// <summary>The MarkAllRead endpoint.</summary>
[WebMcpTool("mark_all_read", "Mark all read.")]
[TypeOption(typeof(MessageEndpoints), "MarkAllRead")]
public class MarkAllReadOption : MessageEndpointBase<MarkAllReadEndpoint>
{
}
