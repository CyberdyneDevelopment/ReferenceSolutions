using Fdw.Collections.Attributes;

namespace ReferenceNfl.Endpoints.NflEndpointOptions;

/// <summary>The RetirePlayer endpoint.</summary>
[TypeOption(typeof(NflEndpoints), "RetirePlayer")]
public class RetirePlayerOption : NflEndpointBase<RetirePlayerEndpoint>
{
}
