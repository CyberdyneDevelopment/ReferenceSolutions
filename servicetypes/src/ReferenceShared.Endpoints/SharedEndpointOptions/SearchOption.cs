using Fdw.Collections.Attributes;

namespace ReferenceShared.Endpoints.SharedEndpointOptions;

/// <summary>The Search endpoint.</summary>
[TypeOption(typeof(SharedEndpoints), "Search")]
public class SearchOption : SharedEndpointBase<SearchEndpoint>
{
}
