using Fdw.Collections.Attributes;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The CreateWebhookProxy endpoint.</summary>
[TypeOption(typeof(ProxyEndpoints), "CreateWebhookProxy")]
public class CreateWebhookProxyOption : ProxyEndpointBase<CreateWebhookProxyEndpoint>
{
}
