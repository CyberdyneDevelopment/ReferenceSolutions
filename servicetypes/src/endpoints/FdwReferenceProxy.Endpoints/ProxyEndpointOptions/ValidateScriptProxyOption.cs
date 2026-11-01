using Fdw.Collections.Attributes;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The ValidateScriptProxy endpoint.</summary>
[TypeOption(typeof(ProxyEndpoints), "ValidateScriptProxy")]
public class ValidateScriptProxyOption : ProxyEndpointBase<ValidateScriptProxyEndpoint>
{
}
