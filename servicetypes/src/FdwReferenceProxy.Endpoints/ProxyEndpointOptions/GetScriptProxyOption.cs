using Fdw.Collections.Attributes;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The GetScriptProxy endpoint.</summary>
[TypeOption(typeof(ProxyEndpoints), "GetScriptProxy")]
public class GetScriptProxyOption : ProxyEndpointBase<GetScriptProxyEndpoint>
{
}
