using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The BulkPipelineStatusProxy endpoint.</summary>
[WebMcpTool("bulk_pipeline_status_proxy", "Get the status of every pipeline (proxy)", ReadOnly = true)]
[TypeOption(typeof(ProxyEndpoints), "BulkPipelineStatusProxy")]
public class BulkPipelineStatusProxyOption : ProxyEndpointBase<BulkPipelineStatusProxyEndpoint>
{
}
