using Fdw.Collections.Attributes;

namespace ReferenceNfl.Endpoints.NflEndpointOptions;

/// <summary>The CreatePlayer endpoint.</summary>
[TypeOption(typeof(NflEndpoints), "CreatePlayer")]
public class CreatePlayerOption : NflEndpointBase<CreatePlayerEndpoint>
{
}
