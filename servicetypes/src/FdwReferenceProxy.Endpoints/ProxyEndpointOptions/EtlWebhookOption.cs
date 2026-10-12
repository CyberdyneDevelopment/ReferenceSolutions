using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The EtlWebhook endpoint.</summary>
[WebMcpTool("etl_webhook", "ETL webhook completion callback")]
[TypeOption(typeof(ProxyEndpoints), "EtlWebhook")]
public class EtlWebhookOption : ProxyEndpointBase<EtlWebhookEndpoint>
{
}
