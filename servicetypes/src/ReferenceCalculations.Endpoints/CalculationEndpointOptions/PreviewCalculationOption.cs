using Fdw.Collections.Attributes;

namespace ReferenceCalculations.Endpoints.CalculationEndpointOptions;

/// <summary>The PreviewCalculation endpoint.</summary>
[TypeOption(typeof(CalculationEndpoints), "PreviewCalculation")]
public class PreviewCalculationOption : CalculationEndpointBase<PreviewCalculationEndpoint>
{
}
