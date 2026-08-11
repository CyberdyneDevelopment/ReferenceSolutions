using Fdw.Collections.Attributes;

namespace ReferencePipelines.Endpoints.PipelineExecutionEndpointOptions;

/// <summary>The GetPipelineExecution endpoint.</summary>
[TypeOption(typeof(PipelineExecutionEndpoints), "GetPipelineExecution")]
public class GetPipelineExecutionOption : PipelineExecutionEndpointBase<GetPipelineExecutionEndpoint>
{
}
