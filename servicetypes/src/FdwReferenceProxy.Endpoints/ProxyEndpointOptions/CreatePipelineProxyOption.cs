using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The CreatePipelineProxy endpoint.</summary>
[WebMcpTool("create_pipeline_proxy", "Create pipeline (proxy)")]
[TypeOption(typeof(ProxyEndpoints), "CreatePipelineProxy")]
public class CreatePipelineProxyOption : ProxyEndpointBase<CreatePipelineProxyEndpoint>
{
}
