using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceMessages.Endpoints.MessageEndpointOptions;

/// <summary>The ListMessages endpoint.</summary>
[WebMcpTool("list_messages", "List messages.", ReadOnly = true)]
[TypeOption(typeof(MessageEndpoints), "ListMessages")]
public class ListMessagesOption : MessageEndpointBase<ListMessagesEndpoint>
{
}
