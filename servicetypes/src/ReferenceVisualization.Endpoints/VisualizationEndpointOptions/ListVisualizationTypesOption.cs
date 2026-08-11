using Fdw.Collections.Attributes;

namespace ReferenceVisualization.Endpoints.VisualizationEndpointOptions;

/// <summary>The ListVisualizationTypes endpoint.</summary>
[TypeOption(typeof(VisualizationEndpoints), "ListVisualizationTypes")]
public class ListVisualizationTypesOption : VisualizationEndpointBase<ListVisualizationTypesEndpoint>
{
}
