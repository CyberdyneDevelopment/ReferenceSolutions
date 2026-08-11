using Fdw.Collections.Attributes;

namespace ReferenceConnections.Endpoints.ConnectionEndpointOptions;

/// <summary>The UpdateConnection endpoint.</summary>
[TypeOption(typeof(ConnectionEndpoints), "UpdateConnection")]
public class UpdateConnectionOption : ConnectionEndpointBase<UpdateConnectionEndpoint>
{
}
