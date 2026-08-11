using Fdw.Collections.Attributes;

namespace ReferenceNfl.Endpoints.NflEndpointOptions;

/// <summary>The ListStandings endpoint.</summary>
[TypeOption(typeof(NflEndpoints), "ListStandings")]
public class ListStandingsOption : NflEndpointBase<ListStandingsEndpoint>
{
}
