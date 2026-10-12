using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The GetEtlExecutionStatusProxy endpoint.</summary>
[WebMcpTool("get_etl_execution_status_proxy", "Get ETL execution status (proxy)", ReadOnly = true)]
[TypeOption(typeof(ProxyEndpoints), "GetEtlExecutionStatusProxy")]
public class GetEtlExecutionStatusProxyOption : ProxyEndpointBase<GetEtlExecutionStatusProxyEndpoint>
{
}
