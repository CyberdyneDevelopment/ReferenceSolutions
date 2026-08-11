using Fdw.Collections.Attributes;

namespace ReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The EtlWebhook endpoint.</summary>
[TypeOption(typeof(ProxyEndpoints), "EtlWebhook")]
public class EtlWebhookOption : ProxyEndpointBase<EtlWebhookEndpoint>
{
}
