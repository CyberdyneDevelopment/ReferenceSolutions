using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceNfl.Endpoints.NflEndpointOptions;

/// <summary>The CreatePlayerGameStat endpoint.</summary>
[WebMcpTool("create_player_game_stat", "Create a player game stat")]
[TypeOption(typeof(NflEndpoints), "CreatePlayerGameStat")]
public class CreatePlayerGameStatOption : NflEndpointBase<CreatePlayerGameStatEndpoint>
{
}
