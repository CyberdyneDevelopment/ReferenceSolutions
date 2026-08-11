using Fdw.Collections.Attributes;

namespace ReferencePipelines.Endpoints.PipelineEndpointOptions;

/// <summary>The ListPipelines endpoint.</summary>
[TypeOption(typeof(PipelineEndpoints), "ListPipelines")]
public class ListPipelinesOption : PipelineEndpointBase<ListPipelinesEndpoint>
{
}
