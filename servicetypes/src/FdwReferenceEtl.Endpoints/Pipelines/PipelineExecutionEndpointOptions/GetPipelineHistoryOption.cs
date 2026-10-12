using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferencePipelines.Endpoints.PipelineExecutionEndpointOptions;

/// <summary>The GetPipelineHistory endpoint.</summary>
[WebMcpTool("get_pipeline_history", "Get pipeline history.", ReadOnly = true)]
[TypeOption(typeof(PipelineExecutionEndpoints), "GetPipelineHistory")]
public class GetPipelineHistoryOption : PipelineExecutionEndpointBase<GetPipelineHistoryEndpoint>
{
}
