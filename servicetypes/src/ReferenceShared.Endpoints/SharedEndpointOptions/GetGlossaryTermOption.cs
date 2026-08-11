using Fdw.Collections.Attributes;

namespace ReferenceShared.Endpoints.SharedEndpointOptions;

/// <summary>The GetGlossaryTerm endpoint.</summary>
[TypeOption(typeof(SharedEndpoints), "GetGlossaryTerm")]
public class GetGlossaryTermOption : SharedEndpointBase<GetGlossaryTermEndpoint>
{
}
