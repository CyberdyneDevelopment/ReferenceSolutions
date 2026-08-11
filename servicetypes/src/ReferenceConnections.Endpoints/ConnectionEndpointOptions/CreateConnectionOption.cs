using Fdw.Collections.Attributes;

namespace ReferenceConnections.Endpoints.ConnectionEndpointOptions;

/// <summary>The CreateConnection endpoint.</summary>
[TypeOption(typeof(ConnectionEndpoints), "CreateConnection")]
public class CreateConnectionOption : ConnectionEndpointBase<CreateConnectionEndpoint>
{
}
