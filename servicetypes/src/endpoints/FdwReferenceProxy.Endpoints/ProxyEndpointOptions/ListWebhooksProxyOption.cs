using Fdw.Collections.Attributes;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The ListWebhooksProxy endpoint.</summary>
[TypeOption(typeof(ProxyEndpoints), "ListWebhooksProxy")]
public class ListWebhooksProxyOption : ProxyEndpointBase<ListWebhooksProxyEndpoint>
{
}
