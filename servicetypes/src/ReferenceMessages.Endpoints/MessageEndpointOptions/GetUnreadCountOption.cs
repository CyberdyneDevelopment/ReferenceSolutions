using Fdw.Collections.Attributes;

namespace ReferenceMessages.Endpoints.MessageEndpointOptions;

/// <summary>The GetUnreadCount endpoint.</summary>
[TypeOption(typeof(MessageEndpoints), "GetUnreadCount")]
public class GetUnreadCountOption : MessageEndpointBase<GetUnreadCountEndpoint>
{
}
