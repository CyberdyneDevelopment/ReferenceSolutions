using Fdw.Collections.Attributes;

namespace ReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The GetEtlExecutionStatusProxy endpoint.</summary>
[TypeOption(typeof(ProxyEndpoints), "GetEtlExecutionStatusProxy")]
public class GetEtlExecutionStatusProxyOption : ProxyEndpointBase<GetEtlExecutionStatusProxyEndpoint>
{
}
