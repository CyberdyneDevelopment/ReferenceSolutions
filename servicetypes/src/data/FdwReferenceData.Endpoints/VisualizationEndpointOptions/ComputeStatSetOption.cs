using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceVisualization.Endpoints.VisualizationEndpointOptions;

/// <summary>The ComputeStatSet endpoint.</summary>
[WebMcpTool("compute_stat_set", "Compute StatSet for a query")]
[TypeOption(typeof(VisualizationEndpoints), "ComputeStatSet")]
public class ComputeStatSetOption : VisualizationEndpointBase<ComputeStatSetEndpoint>
{
}
