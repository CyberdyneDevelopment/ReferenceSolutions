using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceCalculations.Endpoints.CalculationEndpointOptions;

/// <summary>The WindowedCalculation endpoint.</summary>
[WebMcpTool("windowed_calculation", "Execute windowed calculation")]
[TypeOption(typeof(CalculationEndpoints), "WindowedCalculation")]
public class WindowedCalculationOption : CalculationEndpointBase<WindowedCalculationEndpoint>
{
}
