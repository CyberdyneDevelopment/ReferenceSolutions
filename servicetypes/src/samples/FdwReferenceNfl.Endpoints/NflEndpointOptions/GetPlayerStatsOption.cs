using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceNfl.Endpoints.NflEndpointOptions;

/// <summary>The GetPlayerStats endpoint.</summary>
[WebMcpTool("get_player_stats", "Get player stats", ReadOnly = true)]
[TypeOption(typeof(NflEndpoints), "GetPlayerStats")]
public class GetPlayerStatsOption : NflEndpointBase<GetPlayerStatsEndpoint>
{
}
