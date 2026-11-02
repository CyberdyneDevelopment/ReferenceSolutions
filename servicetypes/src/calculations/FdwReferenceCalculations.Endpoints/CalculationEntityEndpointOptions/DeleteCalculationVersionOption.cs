using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceCalculations.Endpoints.CalculationEntityEndpointOptions;

/// <summary>The DeleteCalculationVersion endpoint (FDW-2560).</summary>
[WebMcpTool("delete_calculation_version", "Delete a version of a calculation from an organization or workspace.")]
[TypeOption(typeof(CalculationEntityEndpoints), "DeleteCalculationVersion")]
public class DeleteCalculationVersionOption : CalculationEntityEndpointBase<DeleteCalculationVersionEndpoint>
{
}
