using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSettings.Endpoints.HomePageEndpointOptions;

/// <summary>The PublishTenantHomePage endpoint (FDW-2471).</summary>
[WebMcpTool("publish_tenant_home_page", "Publish the draft of a tenant's home page.")]
[TypeOption(typeof(HomePageEndpoints), "PublishTenantHomePage")]
public class PublishTenantHomePageEndpointOption : HomePageEndpointBase<PublishTenantHomePageEndpoint>
{
}
