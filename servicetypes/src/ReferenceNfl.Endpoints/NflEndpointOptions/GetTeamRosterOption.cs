using Fdw.Collections.Attributes;

namespace ReferenceNfl.Endpoints.NflEndpointOptions;

/// <summary>The GetTeamRoster endpoint.</summary>
[TypeOption(typeof(NflEndpoints), "GetTeamRoster")]
public class GetTeamRosterOption : NflEndpointBase<GetTeamRosterEndpoint>
{
}
