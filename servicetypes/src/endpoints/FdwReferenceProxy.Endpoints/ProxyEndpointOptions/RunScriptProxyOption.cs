using Fdw.Collections.Attributes;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The RunScriptProxy endpoint.</summary>
[TypeOption(typeof(ProxyEndpoints), "RunScriptProxy")]
public class RunScriptProxyOption : ProxyEndpointBase<RunScriptProxyEndpoint>
{
}
