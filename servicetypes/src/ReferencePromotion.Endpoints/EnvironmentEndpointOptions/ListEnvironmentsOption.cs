using Fdw.Collections.Attributes;

namespace ReferencePromotion.Endpoints.EnvironmentEndpointOptions;

/// <summary>The ListEnvironments endpoint.</summary>
[TypeOption(typeof(EnvironmentEndpoints), "ListEnvironments")]
public class ListEnvironmentsOption : EnvironmentEndpointBase<ListEnvironmentsEndpoint>
{
}
