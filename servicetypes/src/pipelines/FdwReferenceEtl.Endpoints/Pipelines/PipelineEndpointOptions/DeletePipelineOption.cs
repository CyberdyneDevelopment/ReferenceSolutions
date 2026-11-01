using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferencePipelines.Endpoints.PipelineEndpointOptions;

/// <summary>The DeletePipeline endpoint.</summary>
[WebMcpTool("delete_pipeline", "Delete pipeline.")]
[TypeOption(typeof(PipelineEndpoints), "DeletePipeline")]
public class DeletePipelineOption : PipelineEndpointBase<DeletePipelineEndpoint>
{
}
