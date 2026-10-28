using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The GetPipelineExecutionProxy endpoint.</summary>
[WebMcpTool("get_pipeline_execution_proxy", "Get the details of a pipeline run (proxy)", ReadOnly = true)]
[TypeOption(typeof(ProxyEndpoints), "GetPipelineExecutionProxy")]
public class GetPipelineExecutionProxyOption : ProxyEndpointBase<GetPipelineExecutionProxyEndpoint>
{
}
