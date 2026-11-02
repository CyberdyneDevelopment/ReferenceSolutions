using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceMessages.Endpoints.MessageEndpointOptions;

/// <summary>The GetMessage endpoint.</summary>
[WebMcpTool("get_message", "Get message.", ReadOnly = true)]
[TypeOption(typeof(MessageEndpoints), "GetMessage")]
public class GetMessageOption : MessageEndpointBase<GetMessageEndpoint>
{
}
