using Fdw.Collections.Attributes;

namespace ReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The GetSchedulesProxy endpoint.</summary>
[TypeOption(typeof(ProxyEndpoints), "GetSchedulesProxy")]
public class GetSchedulesProxyOption : ProxyEndpointBase<GetSchedulesProxyEndpoint>
{
}
