using Fdw.Collections.Attributes;

namespace ReferenceNfl.Endpoints.NflEndpointOptions;

/// <summary>The GetPlayerStats endpoint.</summary>
[TypeOption(typeof(NflEndpoints), "GetPlayerStats")]
public class GetPlayerStatsOption : NflEndpointBase<GetPlayerStatsEndpoint>
{
}
