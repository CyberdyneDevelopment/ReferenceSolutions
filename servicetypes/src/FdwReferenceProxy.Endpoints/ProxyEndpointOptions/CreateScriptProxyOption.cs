using Fdw.Collections.Attributes;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The CreateScriptProxy endpoint.</summary>
[TypeOption(typeof(ProxyEndpoints), "CreateScriptProxy")]
public class CreateScriptProxyOption : ProxyEndpointBase<CreateScriptProxyEndpoint>
{
}
