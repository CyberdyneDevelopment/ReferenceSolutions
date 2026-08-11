using Fdw.Collections.Attributes;

namespace ReferenceCalculations.Endpoints.CalculationEndpointOptions;

/// <summary>The ListCalculationTypes endpoint.</summary>
[TypeOption(typeof(CalculationEndpoints), "ListCalculationTypes")]
public class ListCalculationTypesOption : CalculationEndpointBase<ListCalculationTypesEndpoint>
{
}
