using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferencePipelines.Endpoints.PipelineTypeEndpointOptions;

/// <summary>
/// The endpoints over the pipeline-type resource.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints), "PipelineTypeEndpoints")]
[TypeCollection(typeof(PipelineTypeEndpointBase), typeof(IEndpointTypeOption), typeof(PipelineTypeEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "PipelineTypeEndpoints")]
public partial class PipelineTypeEndpoints : EndpointTypeCollectionBase<PipelineTypeEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
