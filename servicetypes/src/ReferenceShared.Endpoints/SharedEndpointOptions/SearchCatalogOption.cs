using Fdw.Collections.Attributes;

namespace ReferenceShared.Endpoints.SharedEndpointOptions;

/// <summary>The SearchCatalog endpoint.</summary>
[TypeOption(typeof(SharedEndpoints), "SearchCatalog")]
public class SearchCatalogOption : SharedEndpointBase<SearchCatalogEndpoint>
{
}
