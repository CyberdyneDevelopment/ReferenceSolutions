using Fdw.Collections.Attributes;

namespace ReferenceMessages.Endpoints.MessageEndpointOptions;

/// <summary>The GetMessage endpoint.</summary>
[TypeOption(typeof(MessageEndpoints), "GetMessage")]
public class GetMessageOption : MessageEndpointBase<GetMessageEndpoint>
{
}
