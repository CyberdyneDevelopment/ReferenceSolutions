using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceNfl.Endpoints.NflEndpointOptions;

/// <summary>The ListStandings endpoint.</summary>
[WebMcpTool("list_standings", "List season standings", ReadOnly = true)]
[TypeOption(typeof(NflEndpoints), "ListStandings")]
public class ListStandingsOption : NflEndpointBase<ListStandingsEndpoint>
{
}
