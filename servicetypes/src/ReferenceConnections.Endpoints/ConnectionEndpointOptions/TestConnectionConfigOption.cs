using Fdw.Collections.Attributes;

namespace ReferenceConnections.Endpoints.ConnectionEndpointOptions;

/// <summary>The TestConnectionConfig endpoint.</summary>
[TypeOption(typeof(ConnectionEndpoints), "TestConnectionConfig")]
public class TestConnectionConfigOption : ConnectionEndpointBase<TestConnectionConfigEndpoint>
{
}
