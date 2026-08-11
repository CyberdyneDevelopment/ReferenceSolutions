using Fdw.Collections.Attributes;

namespace ReferenceNfl.Endpoints.NflEndpointOptions;

/// <summary>The DeleteStat endpoint.</summary>
[TypeOption(typeof(NflEndpoints), "DeleteStat")]
public class DeleteStatOption : NflEndpointBase<DeleteStatEndpoint>
{
}
