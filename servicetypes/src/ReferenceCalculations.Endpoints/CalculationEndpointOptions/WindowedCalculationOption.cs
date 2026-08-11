using Fdw.Collections.Attributes;

namespace ReferenceCalculations.Endpoints.CalculationEndpointOptions;

/// <summary>The WindowedCalculation endpoint.</summary>
[TypeOption(typeof(CalculationEndpoints), "WindowedCalculation")]
public class WindowedCalculationOption : CalculationEndpointBase<WindowedCalculationEndpoint>
{
}
