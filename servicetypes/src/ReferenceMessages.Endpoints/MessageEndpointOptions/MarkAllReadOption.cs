using Fdw.Collections.Attributes;

namespace ReferenceMessages.Endpoints.MessageEndpointOptions;

/// <summary>The MarkAllRead endpoint.</summary>
[TypeOption(typeof(MessageEndpoints), "MarkAllRead")]
public class MarkAllReadOption : MessageEndpointBase<MarkAllReadEndpoint>
{
}
