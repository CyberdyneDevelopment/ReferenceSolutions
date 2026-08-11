using Fdw.Collections.Attributes;

namespace ReferenceMessages.Endpoints.MessageEndpointOptions;

/// <summary>The DismissMessage endpoint.</summary>
[TypeOption(typeof(MessageEndpoints), "DismissMessage")]
public class DismissMessageOption : MessageEndpointBase<DismissMessageEndpoint>
{
}
