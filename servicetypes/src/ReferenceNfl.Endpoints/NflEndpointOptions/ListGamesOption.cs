using Fdw.Collections.Attributes;

namespace ReferenceNfl.Endpoints.NflEndpointOptions;

/// <summary>The ListGames endpoint.</summary>
[TypeOption(typeof(NflEndpoints), "ListGames")]
public class ListGamesOption : NflEndpointBase<ListGamesEndpoint>
{
}
