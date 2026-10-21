using Fdw.Collections.Attributes;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The UpdateWebhookProxy endpoint.</summary>
[TypeOption(typeof(ProxyEndpoints), "UpdateWebhookProxy")]
public class UpdateWebhookProxyOption : ProxyEndpointBase<UpdateWebhookProxyEndpoint>
{
}
