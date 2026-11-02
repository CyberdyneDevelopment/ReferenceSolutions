using Fdw.Collections.Attributes;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The NewWebhookSecretProxy endpoint.</summary>
[TypeOption(typeof(ProxyEndpoints), "NewWebhookSecretProxy")]
public class NewWebhookSecretProxyOption : ProxyEndpointBase<NewWebhookSecretProxyEndpoint>
{
}
