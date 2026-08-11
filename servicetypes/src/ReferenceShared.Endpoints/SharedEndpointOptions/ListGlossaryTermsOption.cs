using Fdw.Collections.Attributes;

namespace ReferenceShared.Endpoints.SharedEndpointOptions;

/// <summary>The ListGlossaryTerms endpoint.</summary>
[TypeOption(typeof(SharedEndpoints), "ListGlossaryTerms")]
public class ListGlossaryTermsOption : SharedEndpointBase<ListGlossaryTermsEndpoint>
{
}
