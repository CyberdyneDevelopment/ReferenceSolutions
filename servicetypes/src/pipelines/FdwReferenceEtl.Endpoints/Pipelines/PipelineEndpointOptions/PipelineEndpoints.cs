using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using FdwReferenceEndpoints;

namespace FdwReferencePipelines.Endpoints.PipelineEndpointOptions;

/// <summary>
/// The endpoints over the pipeline resource.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "PipelineEndpoints")]
[TypeCollection(typeof(PipelineEndpointBase), typeof(IEndpointTypeOption), typeof(PipelineEndpoints))]
public partial class PipelineEndpoints : EndpointTypeCollectionBase<PipelineEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
