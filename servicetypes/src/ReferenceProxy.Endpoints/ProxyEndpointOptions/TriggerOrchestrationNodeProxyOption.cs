using Fdw.Collections.Attributes;

namespace ReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The TriggerOrchestrationNodeProxy endpoint.</summary>
[TypeOption(typeof(ProxyEndpoints), "TriggerOrchestrationNodeProxy")]
public class TriggerOrchestrationNodeProxyOption : ProxyEndpointBase<TriggerOrchestrationNodeProxyEndpoint>
{
}
