using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using FdwReferenceEndpoints;

namespace FdwReferenceVisualization.Endpoints.VisualizationEndpointOptions;

/// <summary>The endpoints over the visualization surface.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "VisualizationEndpoints")]
[TypeCollection(typeof(VisualizationEndpointBase), typeof(IEndpointTypeOption), typeof(VisualizationEndpoints))]
public partial class VisualizationEndpoints : EndpointTypeCollectionBase<VisualizationEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
