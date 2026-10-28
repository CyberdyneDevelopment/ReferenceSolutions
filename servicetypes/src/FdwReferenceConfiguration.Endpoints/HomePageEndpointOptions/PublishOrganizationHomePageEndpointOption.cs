using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSettings.Endpoints.HomePageEndpointOptions;

/// <summary>The PublishOrganizationHomePage endpoint (FDW-2325).</summary>
[WebMcpTool("publish_organization_home_page", "Publish the draft of an organization's home page.")]
[TypeOption(typeof(HomePageEndpoints), "PublishOrganizationHomePage")]
public class PublishOrganizationHomePageEndpointOption : HomePageEndpointBase<PublishOrganizationHomePageEndpoint>
{
}
