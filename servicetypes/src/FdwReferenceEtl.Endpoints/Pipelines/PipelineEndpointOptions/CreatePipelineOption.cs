using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferencePipelines.Endpoints.PipelineEndpointOptions;

/// <summary>The CreatePipeline endpoint.</summary>
[WebMcpTool("create_pipeline", "Create pipeline.")]
[TypeOption(typeof(PipelineEndpoints), "CreatePipeline")]
public class CreatePipelineOption : PipelineEndpointBase<CreatePipelineEndpoint>
{
}
