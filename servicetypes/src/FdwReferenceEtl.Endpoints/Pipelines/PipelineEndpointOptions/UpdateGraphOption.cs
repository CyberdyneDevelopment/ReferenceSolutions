using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferencePipelines.Endpoints.PipelineEndpointOptions;

/// <summary>The UpdateGraph endpoint.</summary>
[WebMcpTool("update_graph_pipeline", "Replace a graph pipeline.")]
[TypeOption(typeof(PipelineEndpoints), "UpdateGraph")]
public class UpdateGraphOption : PipelineEndpointBase<UpdateGraphEndpoint>
{
}
