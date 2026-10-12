using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceMessages.Endpoints.MessageEndpointOptions;

/// <summary>The GetUnreadCount endpoint.</summary>
[WebMcpTool("get_unread_count", "Get unread count.", ReadOnly = true)]
[TypeOption(typeof(MessageEndpoints), "GetUnreadCount")]
public class GetUnreadCountOption : MessageEndpointBase<GetUnreadCountEndpoint>
{
}
