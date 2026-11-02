using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceConnections.Endpoints.ConnectionEndpointOptions;

/// <summary>The TestConnectionConfig endpoint.</summary>
[WebMcpTool("test_connection_config", "Test a connection configuration")]
[TypeOption(typeof(ConnectionEndpoints), "TestConnectionConfig")]
public class TestConnectionConfigOption : ConnectionEndpointBase<TestConnectionConfigEndpoint>
{
}
