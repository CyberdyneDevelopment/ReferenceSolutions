using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSettings.Endpoints.HomePageEndpointOptions;

/// <summary>The GetOrganizationHomePage endpoint (FDW-2325).</summary>
[WebMcpTool("get_organization_home_page", "Read an organization's published home page and its draft.")]
[TypeOption(typeof(HomePageEndpoints), "GetOrganizationHomePage")]
public class GetOrganizationHomePageEndpointOption : HomePageEndpointBase<GetOrganizationHomePageEndpoint>
{
}
