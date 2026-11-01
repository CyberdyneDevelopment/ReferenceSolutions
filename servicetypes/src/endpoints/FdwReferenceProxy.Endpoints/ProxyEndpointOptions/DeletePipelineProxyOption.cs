using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The DeletePipelineProxy endpoint.</summary>
[WebMcpTool("delete_pipeline_proxy", "Delete pipeline (proxy)")]
[TypeOption(typeof(ProxyEndpoints), "DeletePipelineProxy")]
public class DeletePipelineProxyOption : ProxyEndpointBase<DeletePipelineProxyEndpoint>
{
}
