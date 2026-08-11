using Fdw.Collections.Attributes;

namespace ReferencePipelines.Endpoints.PipelineEndpointOptions;

/// <summary>The GetPipelineStatus endpoint.</summary>
[TypeOption(typeof(PipelineEndpoints), "GetPipelineStatus")]
public class GetPipelineStatusOption : PipelineEndpointBase<GetPipelineStatusEndpoint>
{
}
