using Fdw.Collections.Attributes;

namespace ReferencePromotion.Endpoints.EnvironmentEndpointOptions;

/// <summary>The CompareEnvironments endpoint.</summary>
[TypeOption(typeof(EnvironmentEndpoints), "CompareEnvironments")]
public class CompareEnvironmentsOption : EnvironmentEndpointBase<CompareEnvironmentsEndpoint>
{
}
