using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferencePipelines.Endpoints.PipelineEndpointOptions;

/// <summary>The ListPipelines endpoint.</summary>
[WebMcpTool("list_pipelines", "List pipelines.", ReadOnly = true)]
[TypeOption(typeof(PipelineEndpoints), "ListPipelines")]
public class ListPipelinesOption : PipelineEndpointBase<ListPipelinesEndpoint>
{
}
