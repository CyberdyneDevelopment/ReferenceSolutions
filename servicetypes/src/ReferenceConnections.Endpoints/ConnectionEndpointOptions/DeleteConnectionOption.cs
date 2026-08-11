using Fdw.Collections.Attributes;

namespace ReferenceConnections.Endpoints.ConnectionEndpointOptions;

/// <summary>The DeleteConnection endpoint.</summary>
[TypeOption(typeof(ConnectionEndpoints), "DeleteConnection")]
public class DeleteConnectionOption : ConnectionEndpointBase<DeleteConnectionEndpoint>
{
}
