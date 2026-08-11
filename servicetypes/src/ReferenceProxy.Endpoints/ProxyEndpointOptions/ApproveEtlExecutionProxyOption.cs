using Fdw.Collections.Attributes;

namespace ReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The ApproveEtlExecutionProxy endpoint.</summary>
[TypeOption(typeof(ProxyEndpoints), "ApproveEtlExecutionProxy")]
public class ApproveEtlExecutionProxyOption : ProxyEndpointBase<ApproveEtlExecutionProxyEndpoint>
{
}
