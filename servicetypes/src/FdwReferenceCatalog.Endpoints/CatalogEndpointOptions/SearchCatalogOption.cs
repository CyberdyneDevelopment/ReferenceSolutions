using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceCatalog.Endpoints.CatalogEndpointOptions;

/// <summary>The SearchCatalog endpoint.</summary>
[WebMcpTool("search_catalog", "Search catalog.", ReadOnly = true)]
[TypeOption(typeof(CatalogEndpoints), "SearchCatalog")]
public class SearchCatalogOption : CatalogEndpointBase<SearchCatalogEndpoint>
{
}
