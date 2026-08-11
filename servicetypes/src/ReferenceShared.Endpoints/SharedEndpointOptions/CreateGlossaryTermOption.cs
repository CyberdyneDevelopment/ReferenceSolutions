using Fdw.Collections.Attributes;

namespace ReferenceShared.Endpoints.SharedEndpointOptions;

/// <summary>The CreateGlossaryTerm endpoint.</summary>
[TypeOption(typeof(SharedEndpoints), "CreateGlossaryTerm")]
public class CreateGlossaryTermOption : SharedEndpointBase<CreateGlossaryTermEndpoint>
{
}
