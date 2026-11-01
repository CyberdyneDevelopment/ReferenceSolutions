using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferencePipelines.Endpoints.PipelineEndpointOptions;

/// <summary>The QueryPipelines endpoint.</summary>
[WebMcpTool("query_pipelines", "Query pipelines by filter, order, page and field.", ReadOnly = true)]
[TypeOption(typeof(PipelineEndpoints), "QueryPipelines")]
public class QueryPipelinesOption : PipelineEndpointBase<QueryPipelinesEndpoint>
{
}
