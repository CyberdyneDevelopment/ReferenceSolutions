using Fdw.Collections.Attributes;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The DeleteProjectProxy endpoint.</summary>
[TypeOption(typeof(ProxyEndpoints), "DeleteProjectProxy")]
public class DeleteProjectProxyOption : ProxyEndpointBase<DeleteProjectProxyEndpoint>
{
}
