using Fdw.Collections.Attributes;

namespace ReferencePipelines.Endpoints.PipelineEndpointOptions;

/// <summary>The GetPipelineDetail endpoint.</summary>
[TypeOption(typeof(PipelineEndpoints), "GetPipelineDetail")]
public class GetPipelineDetailOption : PipelineEndpointBase<GetPipelineDetailEndpoint>
{
}
