using Fdw.Collections.Attributes;

namespace ReferenceCalculations.Endpoints.CalculationEntityEndpointOptions;

/// <summary>The CreateCalculationEntity endpoint.</summary>
[TypeOption(typeof(CalculationEntityEndpoints), "CreateCalculationEntity")]
public class CreateCalculationEntityOption : CalculationEntityEndpointBase<CreateCalculationEntityEndpoint>
{
}
