using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceNfl.Endpoints.NflEndpointOptions;

/// <summary>The GetGameBoxscore endpoint.</summary>
[WebMcpTool("get_game_boxscore", "Get game boxscore", ReadOnly = true)]
[TypeOption(typeof(NflEndpoints), "GetGameBoxscore")]
public class GetGameBoxscoreOption : NflEndpointBase<GetGameBoxscoreEndpoint>
{
}
