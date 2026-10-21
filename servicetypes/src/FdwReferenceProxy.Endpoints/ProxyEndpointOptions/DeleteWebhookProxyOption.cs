using Fdw.Collections.Attributes;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The DeleteWebhookProxy endpoint.</summary>
[TypeOption(typeof(ProxyEndpoints), "DeleteWebhookProxy")]
public class DeleteWebhookProxyOption : ProxyEndpointBase<DeleteWebhookProxyEndpoint>
{
}
