using Fdw.Collections.Attributes;

namespace ReferenceCalculations.Endpoints.CalculationEntityEndpointOptions;

/// <summary>The UpdateCalculationEntity endpoint.</summary>
[TypeOption(typeof(CalculationEntityEndpoints), "UpdateCalculationEntity")]
public class UpdateCalculationEntityOption : CalculationEntityEndpointBase<UpdateCalculationEntityEndpoint>
{
}
