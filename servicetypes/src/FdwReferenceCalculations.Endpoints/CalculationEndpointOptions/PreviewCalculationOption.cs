using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceCalculations.Endpoints.CalculationEndpointOptions;

/// <summary>The PreviewCalculation endpoint.</summary>
[WebMcpTool("preview_calculation", "Preview a calculation", ReadOnly = true)]
[TypeOption(typeof(CalculationEndpoints), "PreviewCalculation")]
public class PreviewCalculationOption : CalculationEndpointBase<PreviewCalculationEndpoint>
{
}
