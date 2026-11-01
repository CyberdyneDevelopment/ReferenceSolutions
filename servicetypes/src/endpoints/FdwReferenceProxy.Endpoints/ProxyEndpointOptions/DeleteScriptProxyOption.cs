using Fdw.Collections.Attributes;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The DeleteScriptProxy endpoint.</summary>
[TypeOption(typeof(ProxyEndpoints), "DeleteScriptProxy")]
public class DeleteScriptProxyOption : ProxyEndpointBase<DeleteScriptProxyEndpoint>
{
}
