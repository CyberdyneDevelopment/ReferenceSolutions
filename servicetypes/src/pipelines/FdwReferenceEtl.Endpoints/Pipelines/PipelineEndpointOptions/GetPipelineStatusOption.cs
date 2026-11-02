using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferencePipelines.Endpoints.PipelineEndpointOptions;

/// <summary>The GetPipelineStatus endpoint.</summary>
[WebMcpTool("get_pipeline_status", "Get pipeline status.", ReadOnly = true)]
[TypeOption(typeof(PipelineEndpoints), "GetPipelineStatus")]
public class GetPipelineStatusOption : PipelineEndpointBase<GetPipelineStatusEndpoint>
{
}
