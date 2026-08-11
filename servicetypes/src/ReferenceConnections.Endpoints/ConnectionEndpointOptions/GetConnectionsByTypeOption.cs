using Fdw.Collections.Attributes;

namespace ReferenceConnections.Endpoints.ConnectionEndpointOptions;

/// <summary>The GetConnectionsByType endpoint.</summary>
[TypeOption(typeof(ConnectionEndpoints), "GetConnectionsByType")]
public class GetConnectionsByTypeOption : ConnectionEndpointBase<GetConnectionsByTypeEndpoint>
{
}
