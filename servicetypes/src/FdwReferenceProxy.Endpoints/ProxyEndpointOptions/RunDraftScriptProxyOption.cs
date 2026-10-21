using Fdw.Collections.Attributes;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The RunDraftScriptProxy endpoint.</summary>
[TypeOption(typeof(ProxyEndpoints), "RunDraftScriptProxy")]
public class RunDraftScriptProxyOption : ProxyEndpointBase<RunDraftScriptProxyEndpoint>
{
}
