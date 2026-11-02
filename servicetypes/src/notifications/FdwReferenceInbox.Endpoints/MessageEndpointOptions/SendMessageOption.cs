using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceMessages.Endpoints.MessageEndpointOptions;

/// <summary>The SendMessage endpoint.</summary>
[WebMcpTool("send_message", "Send message.")]
[TypeOption(typeof(MessageEndpoints), "SendMessage")]
public class SendMessageOption : MessageEndpointBase<SendMessageEndpoint>
{
}
