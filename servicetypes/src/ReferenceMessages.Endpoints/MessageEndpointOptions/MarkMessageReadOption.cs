using Fdw.Collections.Attributes;

namespace ReferenceMessages.Endpoints.MessageEndpointOptions;

/// <summary>The MarkMessageRead endpoint.</summary>
[TypeOption(typeof(MessageEndpoints), "MarkMessageRead")]
public class MarkMessageReadOption : MessageEndpointBase<MarkMessageReadEndpoint>
{
}
