using Fdw.Collections.Attributes;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The PauseEtlExecutionProxy endpoint.</summary>
[TypeOption(typeof(ProxyEndpoints), "PauseEtlExecutionProxy")]
public class PauseEtlExecutionProxyOption : ProxyEndpointBase<PauseEtlExecutionProxyEndpoint>
{
}
