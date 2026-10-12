using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The UpdatePipelineProxy endpoint.</summary>
[WebMcpTool("update_pipeline_proxy", "Update pipeline (proxy)")]
[TypeOption(typeof(ProxyEndpoints), "UpdatePipelineProxy")]
public class UpdatePipelineProxyOption : ProxyEndpointBase<UpdatePipelineProxyEndpoint>
{
}
