using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSettings.Endpoints.HomePageEndpointOptions;

/// <summary>The GetPersonHomePage endpoint (FDW-2471).</summary>
[WebMcpTool("get_my_home_page", "Read the signed-in person's own home page and draft.")]
[TypeOption(typeof(HomePageEndpoints), "GetPersonHomePage")]
public class GetPersonHomePageEndpointOption : HomePageEndpointBase<GetPersonHomePageEndpoint>
{
}
