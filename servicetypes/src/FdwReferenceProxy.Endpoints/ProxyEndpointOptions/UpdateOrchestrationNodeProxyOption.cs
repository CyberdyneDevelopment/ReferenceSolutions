using Fdw.Collections.Attributes;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The UpdateOrchestrationNodeProxy endpoint.</summary>
[TypeOption(typeof(ProxyEndpoints), "UpdateOrchestrationNodeProxy")]
public class UpdateOrchestrationNodeProxyOption : ProxyEndpointBase<UpdateOrchestrationNodeProxyEndpoint>
{
}
