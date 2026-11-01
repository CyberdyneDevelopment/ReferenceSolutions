using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceCalculations.Endpoints.CalculationEntityEndpointOptions;

/// <summary>The DeleteCalculationEntity endpoint.</summary>
[WebMcpTool("delete_calculation_entity", "Delete calculation entity.")]
[TypeOption(typeof(CalculationEntityEndpoints), "DeleteCalculationEntity")]
public class DeleteCalculationEntityOption : CalculationEntityEndpointBase<DeleteCalculationEntityEndpoint>
{
}
