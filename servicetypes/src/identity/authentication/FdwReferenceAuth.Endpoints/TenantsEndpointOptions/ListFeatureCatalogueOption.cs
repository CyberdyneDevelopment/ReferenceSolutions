using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The ListFeatureCatalogue endpoint (FDW-1580).</summary>
[WebMcpTool("list_features", "List the feature catalogue.")]
[TypeOption(typeof(TenantsEndpoints), "ListFeatureCatalogue")]
public class ListFeatureCatalogueOption : TenantsEndpointBase<ListFeatureCatalogueEndpoint>
{
}
