using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The TriggerEtlJobProxy endpoint.</summary>
[WebMcpTool("trigger_etl_job_proxy", "Trigger ETL job (proxy)")]
[TypeOption(typeof(ProxyEndpoints), "TriggerEtlJobProxy")]
public class TriggerEtlJobProxyOption : ProxyEndpointBase<TriggerEtlJobProxyEndpoint>
{
}
