using Fdw.Collections.Attributes;

namespace ReferenceConnections.Endpoints.ConnectionEndpointOptions;

/// <summary>The UpdateHttpConnection endpoint.</summary>
[TypeOption(typeof(ConnectionEndpoints), "UpdateHttpConnection")]
public class UpdateHttpConnectionOption : ConnectionEndpointBase<UpdateHttpConnectionEndpoint>
{
}
