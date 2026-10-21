using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferencePipelines.Endpoints.PipelineEndpointOptions;

/// <summary>The CreateGraph endpoint.</summary>
[WebMcpTool("create_graph_pipeline", "Create a graph pipeline.")]
[TypeOption(typeof(PipelineEndpoints), "CreateGraph")]
public class CreateGraphOption : PipelineEndpointBase<CreateGraphEndpoint>
{
}
