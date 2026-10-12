using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferencePipelines.Endpoints.PipelineEndpointOptions;

/// <summary>The UpdatePipeline endpoint.</summary>
[WebMcpTool("update_pipeline", "Update pipeline.")]
[TypeOption(typeof(PipelineEndpoints), "UpdatePipeline")]
public class UpdatePipelineOption : PipelineEndpointBase<UpdatePipelineEndpoint>
{
}
