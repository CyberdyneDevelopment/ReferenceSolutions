using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferencePipelines.Endpoints.PipelineEndpointOptions;

/// <summary>The GetGraph endpoint.</summary>
[WebMcpTool("get_graph_pipeline", "Get a graph pipeline.", ReadOnly = true)]
[TypeOption(typeof(PipelineEndpoints), "GetGraph")]
public class GetGraphOption : PipelineEndpointBase<GetGraphEndpoint>
{
}
