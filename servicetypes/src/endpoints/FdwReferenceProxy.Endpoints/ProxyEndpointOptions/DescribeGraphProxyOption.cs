using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The DescribeGraphProxy endpoint.</summary>
[WebMcpTool("describe_graph_pipeline_proxy", "Describe graph pipeline draft (proxy)", ReadOnly = true)]
[TypeOption(typeof(ProxyEndpoints), "DescribeGraphProxy")]
public class DescribeGraphProxyOption : ProxyEndpointBase<DescribeGraphProxyEndpoint>
{
}
