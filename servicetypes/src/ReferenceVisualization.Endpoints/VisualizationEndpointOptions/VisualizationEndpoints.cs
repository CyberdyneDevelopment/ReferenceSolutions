using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceEndpoints;

namespace ReferenceVisualization.Endpoints.VisualizationEndpointOptions;

/// <summary>The endpoints over the visualization surface.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "VisualizationEndpoints")]
[TypeCollection(typeof(VisualizationEndpointBase), typeof(IEndpointTypeOption), typeof(VisualizationEndpoints))]
public partial class VisualizationEndpoints : EndpointTypeCollectionBase<VisualizationEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
