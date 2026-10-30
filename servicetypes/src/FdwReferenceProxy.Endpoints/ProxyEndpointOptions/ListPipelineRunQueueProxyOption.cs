using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The ListPipelineRunQueueProxy endpoint.</summary>
[WebMcpTool("list_pipeline_run_queue_proxy", "List the pipeline run queue (proxy)", ReadOnly = true)]
[TypeOption(typeof(ProxyEndpoints), "ListPipelineRunQueueProxy")]
public class ListPipelineRunQueueProxyOption : ProxyEndpointBase<ListPipelineRunQueueProxyEndpoint>
{
}
