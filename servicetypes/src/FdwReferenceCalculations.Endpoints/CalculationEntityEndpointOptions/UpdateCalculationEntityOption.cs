using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceCalculations.Endpoints.CalculationEntityEndpointOptions;

/// <summary>The UpdateCalculationEntity endpoint.</summary>
[WebMcpTool("update_calculation_entity", "Update calculation entity.")]
[TypeOption(typeof(CalculationEntityEndpoints), "UpdateCalculationEntity")]
public class UpdateCalculationEntityOption : CalculationEntityEndpointBase<UpdateCalculationEntityEndpoint>
{
}
