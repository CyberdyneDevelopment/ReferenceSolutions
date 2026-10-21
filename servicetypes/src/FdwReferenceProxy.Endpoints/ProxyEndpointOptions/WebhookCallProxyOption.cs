using Fdw.Collections.Attributes;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The WebhookCallProxy endpoint: the address an outside system calls to start a pipeline (FDW-1884).</summary>
[TypeOption(typeof(ProxyEndpoints), "WebhookCallProxy")]
public class WebhookCallProxyOption : ProxyEndpointBase<WebhookCallProxyEndpoint>
{
}
