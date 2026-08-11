using Fdw.Collections.Attributes;

namespace ReferencePipelines.Endpoints.PipelineExecutionEndpointOptions;

/// <summary>The GetPipelineHistory endpoint.</summary>
[TypeOption(typeof(PipelineExecutionEndpoints), "GetPipelineHistory")]
public class GetPipelineHistoryOption : PipelineExecutionEndpointBase<GetPipelineHistoryEndpoint>
{
}
