using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSettings.Endpoints.HomePageEndpointOptions;

/// <summary>The PublishPersonHomePage endpoint (FDW-2471).</summary>
[WebMcpTool("publish_my_home_page", "Publish the draft of the signed-in person's own home page.")]
[TypeOption(typeof(HomePageEndpoints), "PublishPersonHomePage")]
public class PublishPersonHomePageEndpointOption : HomePageEndpointBase<PublishPersonHomePageEndpoint>
{
}
