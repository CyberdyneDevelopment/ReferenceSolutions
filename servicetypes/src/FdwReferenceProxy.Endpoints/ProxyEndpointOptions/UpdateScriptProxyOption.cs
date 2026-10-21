using Fdw.Collections.Attributes;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The UpdateScriptProxy endpoint.</summary>
[TypeOption(typeof(ProxyEndpoints), "UpdateScriptProxy")]
public class UpdateScriptProxyOption : ProxyEndpointBase<UpdateScriptProxyEndpoint>
{
}
