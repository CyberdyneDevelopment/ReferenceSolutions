using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The LookupPipelinesProxy endpoint.</summary>
[WebMcpTool("lookup_pipelines_proxy", "Look up pipelines by name (proxy)", ReadOnly = true)]
[TypeOption(typeof(ProxyEndpoints), "LookupPipelinesProxy")]
public class LookupPipelinesProxyOption : ProxyEndpointBase<LookupPipelinesProxyEndpoint>
{
}
