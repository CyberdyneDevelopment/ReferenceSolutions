using Fdw.Collections.Attributes;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The DenyEtlExecutionProxy endpoint (FDW-1740). Not offered as an agent tool: a decision is a person's.</summary>
[TypeOption(typeof(ProxyEndpoints), "DenyEtlExecutionProxy")]
public class DenyEtlExecutionProxyOption : ProxyEndpointBase<DenyEtlExecutionProxyEndpoint>
{
}
