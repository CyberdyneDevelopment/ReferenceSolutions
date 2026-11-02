using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferencePipelines.Endpoints.PipelineEndpointOptions;

/// <summary>The ExecutePipeline endpoint.</summary>
[WebMcpTool("execute_pipeline", "Execute pipeline.")]
[TypeOption(typeof(PipelineEndpoints), "ExecutePipeline")]
public class ExecutePipelineOption : PipelineEndpointBase<ExecutePipelineEndpoint>
{
}
