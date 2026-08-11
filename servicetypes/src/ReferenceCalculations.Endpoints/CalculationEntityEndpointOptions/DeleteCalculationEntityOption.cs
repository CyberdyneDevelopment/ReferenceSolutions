using Fdw.Collections.Attributes;

namespace ReferenceCalculations.Endpoints.CalculationEntityEndpointOptions;

/// <summary>The DeleteCalculationEntity endpoint.</summary>
[TypeOption(typeof(CalculationEntityEndpoints), "DeleteCalculationEntity")]
public class DeleteCalculationEntityOption : CalculationEntityEndpointBase<DeleteCalculationEntityEndpoint>
{
}
