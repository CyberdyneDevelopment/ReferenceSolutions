using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The TriggerPipelineProxy endpoint.</summary>
[WebMcpTool("trigger_pipeline_proxy", "Trigger ETL pipeline (unified route)")]
[TypeOption(typeof(ProxyEndpoints), "TriggerPipelineProxy")]
public class TriggerPipelineProxyOption : ProxyEndpointBase<TriggerPipelineProxyEndpoint>
{
}
