using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceNfl.Endpoints.NflEndpointOptions;

/// <summary>The GetTeamRoster endpoint.</summary>
[WebMcpTool("get_team_roster", "Get team roster", ReadOnly = true)]
[TypeOption(typeof(NflEndpoints), "GetTeamRoster")]
public class GetTeamRosterOption : NflEndpointBase<GetTeamRosterEndpoint>
{
}
