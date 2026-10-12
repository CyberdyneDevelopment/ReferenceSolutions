using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using FdwReferenceEndpoints;

namespace FdwReferencePipelines.Endpoints.PipelineExecutionEndpointOptions;

/// <summary>
/// The endpoints over the pipeline-execution resource.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "PipelineExecutionEndpoints")]
[TypeCollection(typeof(PipelineExecutionEndpointBase), typeof(IEndpointTypeOption), typeof(PipelineExecutionEndpoints))]
public partial class PipelineExecutionEndpoints : EndpointTypeCollectionBase<PipelineExecutionEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
