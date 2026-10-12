using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferencePipelines.Endpoints.PipelineEndpointOptions;

/// <summary>The LookupPipelines endpoint.</summary>
[WebMcpTool("lookup_pipelines", "Look up pipelines by name.", ReadOnly = true)]
[TypeOption(typeof(PipelineEndpoints), "LookupPipelines")]
public class LookupPipelinesOption : PipelineEndpointBase<LookupPipelinesEndpoint>
{
}
