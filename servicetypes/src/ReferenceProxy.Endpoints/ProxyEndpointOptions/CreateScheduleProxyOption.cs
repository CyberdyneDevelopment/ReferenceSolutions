using Fdw.Collections.Attributes;

namespace ReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The CreateScheduleProxy endpoint.</summary>
[TypeOption(typeof(ProxyEndpoints), "CreateScheduleProxy")]
public class CreateScheduleProxyOption : ProxyEndpointBase<CreateScheduleProxyEndpoint>
{
}
