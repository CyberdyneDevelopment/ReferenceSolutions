using Fdw.Collections.Attributes;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The UpdateProjectProxy endpoint.</summary>
[TypeOption(typeof(ProxyEndpoints), "UpdateProjectProxy")]
public class UpdateProjectProxyOption : ProxyEndpointBase<UpdateProjectProxyEndpoint>
{
}
