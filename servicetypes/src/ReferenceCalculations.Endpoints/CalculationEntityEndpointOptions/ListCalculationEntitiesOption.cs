using Fdw.Collections.Attributes;

namespace ReferenceCalculations.Endpoints.CalculationEntityEndpointOptions;

/// <summary>The ListCalculationEntities endpoint.</summary>
[TypeOption(typeof(CalculationEntityEndpoints), "ListCalculationEntities")]
public class ListCalculationEntitiesOption : CalculationEntityEndpointBase<ListCalculationEntitiesEndpoint>
{
}
