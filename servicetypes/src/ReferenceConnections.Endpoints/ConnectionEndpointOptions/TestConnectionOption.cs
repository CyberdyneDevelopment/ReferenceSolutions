using Fdw.Collections.Attributes;

namespace ReferenceConnections.Endpoints.ConnectionEndpointOptions;

/// <summary>The TestConnection endpoint.</summary>
[TypeOption(typeof(ConnectionEndpoints), "TestConnection")]
public class TestConnectionOption : ConnectionEndpointBase<TestConnectionEndpoint>
{
}
