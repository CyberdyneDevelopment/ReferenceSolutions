using Fdw.Collections.Attributes;

namespace ReferenceMessages.Endpoints.MessageEndpointOptions;

/// <summary>The ListMessages endpoint.</summary>
[TypeOption(typeof(MessageEndpoints), "ListMessages")]
public class ListMessagesOption : MessageEndpointBase<ListMessagesEndpoint>
{
}
