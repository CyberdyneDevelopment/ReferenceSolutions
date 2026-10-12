using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceCalculations.Endpoints.CalculationEntityEndpointOptions;

/// <summary>The CreateCalculationEntity endpoint.</summary>
[WebMcpTool("create_calculation_entity", "Create calculation entity.")]
[TypeOption(typeof(CalculationEntityEndpoints), "CreateCalculationEntity")]
public class CreateCalculationEntityOption : CalculationEntityEndpointBase<CreateCalculationEntityEndpoint>
{
}
