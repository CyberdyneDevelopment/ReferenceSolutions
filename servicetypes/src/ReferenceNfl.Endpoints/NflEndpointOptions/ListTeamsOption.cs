using Fdw.Collections.Attributes;

namespace ReferenceNfl.Endpoints.NflEndpointOptions;

/// <summary>The ListTeams endpoint.</summary>
[TypeOption(typeof(NflEndpoints), "ListTeams")]
public class ListTeamsOption : NflEndpointBase<ListTeamsEndpoint>
{
}
