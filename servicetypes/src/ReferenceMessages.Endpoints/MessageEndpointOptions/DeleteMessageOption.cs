using Fdw.Collections.Attributes;

namespace ReferenceMessages.Endpoints.MessageEndpointOptions;

/// <summary>The DeleteMessage endpoint.</summary>
[TypeOption(typeof(MessageEndpoints), "DeleteMessage")]
public class DeleteMessageOption : MessageEndpointBase<DeleteMessageEndpoint>
{
}
