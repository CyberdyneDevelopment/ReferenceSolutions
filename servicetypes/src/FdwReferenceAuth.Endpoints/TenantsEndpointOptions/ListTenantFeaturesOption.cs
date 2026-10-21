using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The ListTenantFeatures endpoint (FDW-1580).</summary>
[WebMcpTool("list_tenant_features", "List the features of a tenant.")]
[TypeOption(typeof(TenantsEndpoints), "ListTenantFeatures")]
public class ListTenantFeaturesOption : TenantsEndpointBase<ListTenantFeaturesEndpoint>
{
}
