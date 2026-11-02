using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceVisualization.Endpoints.VisualizationEndpointOptions;

/// <summary>The ListVisualizationTypes endpoint.</summary>
[WebMcpTool("list_visualization_types", "List available visualization types", ReadOnly = true)]
[TypeOption(typeof(VisualizationEndpoints), "ListVisualizationTypes")]
public class ListVisualizationTypesOption : VisualizationEndpointBase<ListVisualizationTypesEndpoint>
{
}
