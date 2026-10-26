using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceCalculations.Endpoints.CalculationEntityEndpointOptions;

/// <summary>The SaveCalculationVersion endpoint (FDW-2560).</summary>
[WebMcpTool("save_calculation_version", "Save a version of a calculation at an organization or workspace.")]
[TypeOption(typeof(CalculationEntityEndpoints), "SaveCalculationVersion")]
public class SaveCalculationVersionOption : CalculationEntityEndpointBase<SaveCalculationVersionEndpoint>
{
}
