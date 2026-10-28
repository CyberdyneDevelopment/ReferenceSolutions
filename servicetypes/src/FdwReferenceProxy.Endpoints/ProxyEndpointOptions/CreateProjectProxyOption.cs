using Fdw.Collections.Attributes;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The CreateProjectProxy endpoint.</summary>
[TypeOption(typeof(ProxyEndpoints), "CreateProjectProxy")]
public class CreateProjectProxyOption : ProxyEndpointBase<CreateProjectProxyEndpoint>
{
}
