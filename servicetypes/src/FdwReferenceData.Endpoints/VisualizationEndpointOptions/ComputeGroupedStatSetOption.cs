using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceVisualization.Endpoints.VisualizationEndpointOptions;

/// <summary>The ComputeGroupedStatSet endpoint.</summary>
[WebMcpTool("compute_grouped_stat_set", "Compute grouped StatSet with dimensions")]
[TypeOption(typeof(VisualizationEndpoints), "ComputeGroupedStatSet")]
public class ComputeGroupedStatSetOption : VisualizationEndpointBase<ComputeGroupedStatSetEndpoint>
{
}
