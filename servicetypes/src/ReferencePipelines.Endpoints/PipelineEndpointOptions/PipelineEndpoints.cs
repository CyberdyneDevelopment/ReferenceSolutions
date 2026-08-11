using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferencePipelines.Endpoints.PipelineEndpointOptions;

/// <summary>
/// The endpoints over the pipeline resource.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(PipelineEndpointBase), typeof(IEndpointTypeOption), typeof(PipelineEndpoints))]
public partial class PipelineEndpoints : EndpointTypeCollectionBase<PipelineEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
