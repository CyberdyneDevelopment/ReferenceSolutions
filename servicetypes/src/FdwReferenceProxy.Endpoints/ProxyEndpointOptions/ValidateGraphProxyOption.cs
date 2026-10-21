using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The ValidateGraphProxy endpoint.</summary>
[WebMcpTool("validate_graph_pipeline_proxy", "Check graph pipeline draft (proxy)", ReadOnly = true)]
[TypeOption(typeof(ProxyEndpoints), "ValidateGraphProxy")]
public class ValidateGraphProxyOption : ProxyEndpointBase<ValidateGraphProxyEndpoint>
{
}
