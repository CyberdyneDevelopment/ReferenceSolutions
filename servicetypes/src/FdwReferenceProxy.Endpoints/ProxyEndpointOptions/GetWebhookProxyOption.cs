using Fdw.Collections.Attributes;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The GetWebhookProxy endpoint.</summary>
[TypeOption(typeof(ProxyEndpoints), "GetWebhookProxy")]
public class GetWebhookProxyOption : ProxyEndpointBase<GetWebhookProxyEndpoint>
{
}
