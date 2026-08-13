using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceVisualization.Endpoints.VisualizationEndpointOptions;

/// <summary>The endpoints over the visualization surface.</summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints), "VisualizationEndpoints")]
[TypeCollection(typeof(VisualizationEndpointBase), typeof(IEndpointTypeOption), typeof(VisualizationEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "VisualizationEndpoints")]
public partial class VisualizationEndpoints : EndpointTypeCollectionBase<VisualizationEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
