using Fdw.Collections.Attributes;

namespace ReferenceConnections.Endpoints.ConnectionTypeEndpointOptions;

/// <summary>The GetConnectionTypeCapabilities endpoint.</summary>
[TypeOption(typeof(ConnectionTypeEndpoints), "GetConnectionTypeCapabilities")]
public class GetConnectionTypeCapabilitiesOption : ConnectionTypeEndpointBase<GetConnectionTypeCapabilitiesEndpoint>
{
}
