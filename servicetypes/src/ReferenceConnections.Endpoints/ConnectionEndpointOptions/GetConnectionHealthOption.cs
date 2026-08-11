using Fdw.Collections.Attributes;

namespace ReferenceConnections.Endpoints.ConnectionEndpointOptions;

/// <summary>The GetConnectionHealth endpoint.</summary>
[TypeOption(typeof(ConnectionEndpoints), "GetConnectionHealth")]
public class GetConnectionHealthOption : ConnectionEndpointBase<GetConnectionHealthEndpoint>
{
}
