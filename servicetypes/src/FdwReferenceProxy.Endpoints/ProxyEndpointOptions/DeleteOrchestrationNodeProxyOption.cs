using Fdw.Collections.Attributes;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The DeleteOrchestrationNodeProxy endpoint.</summary>
[TypeOption(typeof(ProxyEndpoints), "DeleteOrchestrationNodeProxy")]
public class DeleteOrchestrationNodeProxyOption : ProxyEndpointBase<DeleteOrchestrationNodeProxyEndpoint>
{
}
