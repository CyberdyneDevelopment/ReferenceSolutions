using Fdw.Collections.Attributes;

namespace ReferenceConnections.Endpoints.ConnectionEndpointOptions;

/// <summary>The GetConnection endpoint.</summary>
[TypeOption(typeof(ConnectionEndpoints), "GetConnection")]
public class GetConnectionOption : ConnectionEndpointBase<GetConnectionEndpoint>
{
}
