using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferencePipelines.Endpoints.PipelineTypeEndpointOptions;

/// <summary>
/// The endpoints over the pipeline-type resource.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(PipelineTypeEndpointBase), typeof(IEndpointTypeOption), typeof(PipelineTypeEndpoints))]
public partial class PipelineTypeEndpoints : EndpointTypeCollectionBase<PipelineTypeEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
