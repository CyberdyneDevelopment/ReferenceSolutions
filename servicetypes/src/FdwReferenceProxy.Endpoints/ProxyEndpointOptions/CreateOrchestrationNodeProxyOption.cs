using Fdw.Collections.Attributes;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The CreateOrchestrationNodeProxy endpoint.</summary>
[TypeOption(typeof(ProxyEndpoints), "CreateOrchestrationNodeProxy")]
public class CreateOrchestrationNodeProxyOption : ProxyEndpointBase<CreateOrchestrationNodeProxyEndpoint>
{
}
