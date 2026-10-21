using Fdw.Collections.Attributes;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The ListScriptsProxy endpoint.</summary>
[TypeOption(typeof(ProxyEndpoints), "ListScriptsProxy")]
public class ListScriptsProxyOption : ProxyEndpointBase<ListScriptsProxyEndpoint>
{
}
