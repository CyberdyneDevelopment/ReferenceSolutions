using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceCatalog.Endpoints.CatalogEndpointOptions;

/// <summary>The GetCatalogUses endpoint.</summary>
[WebMcpTool("get_catalog_uses", "Where a data set, container, pipeline, calculation or term is used, or what it uses.", ReadOnly = true)]
[TypeOption(typeof(CatalogEndpoints), "GetCatalogUses")]
public class GetCatalogUsesOption : CatalogEndpointBase<GetCatalogUsesEndpoint>
{
}
