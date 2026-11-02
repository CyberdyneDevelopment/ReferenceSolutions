using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceNfl.Endpoints.NflEndpointOptions;

/// <summary>The ListGames endpoint.</summary>
[WebMcpTool("list_games", "List NFL games", ReadOnly = true)]
[TypeOption(typeof(NflEndpoints), "ListGames")]
public class ListGamesOption : NflEndpointBase<ListGamesEndpoint>
{
}
