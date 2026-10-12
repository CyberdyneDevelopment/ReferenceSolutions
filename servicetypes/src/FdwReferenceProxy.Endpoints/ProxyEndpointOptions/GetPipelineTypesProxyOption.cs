using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The GetPipelineTypesProxy endpoint.</summary>
[WebMcpTool("get_pipeline_types_proxy", "List pipeline engine types (proxy)", ReadOnly = true)]
[TypeOption(typeof(ProxyEndpoints), "GetPipelineTypesProxy")]
public class GetPipelineTypesProxyOption : ProxyEndpointBase<GetPipelineTypesProxyEndpoint>
{
}
