using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferencePipelines.Endpoints.PipelineExecutionEndpointOptions;

/// <summary>The GetPipelineExecution endpoint.</summary>
[WebMcpTool("get_pipeline_execution", "Get pipeline execution.", ReadOnly = true)]
[TypeOption(typeof(PipelineExecutionEndpoints), "GetPipelineExecution")]
public class GetPipelineExecutionOption : PipelineExecutionEndpointBase<GetPipelineExecutionEndpoint>
{
}
