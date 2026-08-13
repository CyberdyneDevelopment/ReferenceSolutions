using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceVisualization.Endpoints.VisualizationEndpointOptions;

/// <summary>The endpoints over the visualization surface.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.EndpointGroups), "VisualizationEndpoints")]
[TypeCollection(typeof(VisualizationEndpointBase), typeof(IEndpointTypeOption), typeof(VisualizationEndpoints))]
public partial class VisualizationEndpoints : EndpointTypeCollectionBase<VisualizationEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
