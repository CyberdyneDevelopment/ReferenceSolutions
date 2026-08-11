using Fdw.Collections.Attributes;

namespace ReferenceShared.Endpoints.SharedEndpointOptions;

/// <summary>The DeleteGlossaryTerm endpoint.</summary>
[TypeOption(typeof(SharedEndpoints), "DeleteGlossaryTerm")]
public class DeleteGlossaryTermOption : SharedEndpointBase<DeleteGlossaryTermEndpoint>
{
}
