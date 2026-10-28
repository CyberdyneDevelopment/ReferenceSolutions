using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSettings.Endpoints.HomePageEndpointOptions;

/// <summary>The GetHomePage endpoint (FDW-2325).</summary>
[WebMcpTool("get_home_page", "Read the home page that applies to the caller.")]
[TypeOption(typeof(HomePageEndpoints), "GetHomePage")]
public class GetHomePageEndpointOption : HomePageEndpointBase<GetHomePageEndpoint>
{
}
