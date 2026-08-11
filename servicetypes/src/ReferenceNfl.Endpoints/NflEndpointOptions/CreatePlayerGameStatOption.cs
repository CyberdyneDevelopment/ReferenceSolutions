using Fdw.Collections.Attributes;

namespace ReferenceNfl.Endpoints.NflEndpointOptions;

/// <summary>The CreatePlayerGameStat endpoint.</summary>
[TypeOption(typeof(NflEndpoints), "CreatePlayerGameStat")]
public class CreatePlayerGameStatOption : NflEndpointBase<CreatePlayerGameStatEndpoint>
{
}
