using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferencePipelines.Endpoints.PipelineExecutionEndpointOptions;

/// <summary>
/// The endpoints over the pipeline-execution resource.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(PipelineExecutionEndpointBase), typeof(IEndpointTypeOption), typeof(PipelineExecutionEndpoints))]
public partial class PipelineExecutionEndpoints : EndpointTypeCollectionBase<PipelineExecutionEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
