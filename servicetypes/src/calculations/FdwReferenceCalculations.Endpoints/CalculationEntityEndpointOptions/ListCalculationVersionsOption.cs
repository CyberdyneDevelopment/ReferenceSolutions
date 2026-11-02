using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceCalculations.Endpoints.CalculationEntityEndpointOptions;

/// <summary>The ListCalculationVersions endpoint (FDW-2560).</summary>
[WebMcpTool("list_calculation_versions", "List the versions of a calculation at the places the caller may read.", ReadOnly = true)]
[TypeOption(typeof(CalculationEntityEndpoints), "ListCalculationVersions")]
public class ListCalculationVersionsOption : CalculationEntityEndpointBase<ListCalculationVersionsEndpoint>
{
}
