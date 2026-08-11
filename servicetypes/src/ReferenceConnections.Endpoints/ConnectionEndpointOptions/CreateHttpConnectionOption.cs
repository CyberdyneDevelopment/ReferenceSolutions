using Fdw.Collections.Attributes;

namespace ReferenceConnections.Endpoints.ConnectionEndpointOptions;

/// <summary>The CreateHttpConnection endpoint.</summary>
[TypeOption(typeof(ConnectionEndpoints), "CreateHttpConnection")]
public class CreateHttpConnectionOption : ConnectionEndpointBase<CreateHttpConnectionEndpoint>
{
}
