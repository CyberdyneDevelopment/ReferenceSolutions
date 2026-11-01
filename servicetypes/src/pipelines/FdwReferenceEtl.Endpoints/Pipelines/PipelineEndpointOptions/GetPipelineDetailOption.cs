using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferencePipelines.Endpoints.PipelineEndpointOptions;

/// <summary>The GetPipelineDetail endpoint.</summary>
[WebMcpTool("get_pipeline_detail", "Get pipeline detail.", ReadOnly = true)]
[TypeOption(typeof(PipelineEndpoints), "GetPipelineDetail")]
public class GetPipelineDetailOption : PipelineEndpointBase<GetPipelineDetailEndpoint>
{
}
