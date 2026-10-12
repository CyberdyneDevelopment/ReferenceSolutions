using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceNfl.Endpoints.NflEndpointOptions;

/// <summary>The ListTeams endpoint.</summary>
[WebMcpTool("list_teams", "List NFL teams", ReadOnly = true)]
[TypeOption(typeof(NflEndpoints), "ListTeams")]
public class ListTeamsOption : NflEndpointBase<ListTeamsEndpoint>
{
}
