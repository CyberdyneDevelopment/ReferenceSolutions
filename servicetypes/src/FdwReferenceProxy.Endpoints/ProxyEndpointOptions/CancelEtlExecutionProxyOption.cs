using Fdw.Collections.Attributes;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The CancelEtlExecutionProxy endpoint.</summary>
[TypeOption(typeof(ProxyEndpoints), "CancelEtlExecutionProxy")]
public class CancelEtlExecutionProxyOption : ProxyEndpointBase<CancelEtlExecutionProxyEndpoint>
{
}
