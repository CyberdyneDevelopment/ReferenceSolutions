using Fdw.Collections.Attributes;

namespace ReferenceNfl.Endpoints.NflEndpointOptions;

/// <summary>The GetGameBoxscore endpoint.</summary>
[TypeOption(typeof(NflEndpoints), "GetGameBoxscore")]
public class GetGameBoxscoreOption : NflEndpointBase<GetGameBoxscoreEndpoint>
{
}
