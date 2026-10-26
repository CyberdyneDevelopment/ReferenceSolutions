using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceMessages.Endpoints.MessageEndpointOptions;

/// <summary>The DismissMessage endpoint.</summary>
[WebMcpTool("dismiss_message", "Dismiss message.")]
[TypeOption(typeof(MessageEndpoints), "DismissMessage")]
public class DismissMessageOption : MessageEndpointBase<DismissMessageEndpoint>
{
}
