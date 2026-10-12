using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The ApproveEtlExecutionProxy endpoint.</summary>
[WebMcpTool("approve_etl_execution_proxy", "Approve a paused ETL execution (proxy)")]
[TypeOption(typeof(ProxyEndpoints), "ApproveEtlExecutionProxy")]
public class ApproveEtlExecutionProxyOption : ProxyEndpointBase<ApproveEtlExecutionProxyEndpoint>
{
}
