using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSettings.Endpoints.HomePageEndpointOptions;

/// <summary>The GetTenantHomePage endpoint (FDW-2471).</summary>
[WebMcpTool("get_tenant_home_page", "Read a tenant's published home page and its draft.")]
[TypeOption(typeof(HomePageEndpoints), "GetTenantHomePage")]
public class GetTenantHomePageEndpointOption : HomePageEndpointBase<GetTenantHomePageEndpoint>
{
}
