using Fdw.Collections.Attributes;

namespace ReferenceConnections.Endpoints.ConnectionEndpointOptions;

/// <summary>The ListConnections endpoint.</summary>
[TypeOption(typeof(ConnectionEndpoints), "ListConnections")]
public class ListConnectionsOption : ConnectionEndpointBase<ListConnectionsEndpoint>
{
}
