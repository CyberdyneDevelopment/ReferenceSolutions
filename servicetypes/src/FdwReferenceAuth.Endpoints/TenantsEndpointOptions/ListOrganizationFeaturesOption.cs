using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The ListOrganizationFeatures endpoint (FDW-1580).</summary>
[WebMcpTool("list_organization_features", "List the features of an organization.")]
[TypeOption(typeof(TenantsEndpoints), "ListOrganizationFeatures")]
public class ListOrganizationFeaturesOption : TenantsEndpointBase<ListOrganizationFeaturesEndpoint>
{
}
