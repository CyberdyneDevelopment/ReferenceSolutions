using Fdw.Collections.Attributes;

namespace ReferenceMessages.Endpoints.MessageEndpointOptions;

/// <summary>The ArchiveMessage endpoint.</summary>
[TypeOption(typeof(MessageEndpoints), "ArchiveMessage")]
public class ArchiveMessageOption : MessageEndpointBase<ArchiveMessageEndpoint>
{
}
