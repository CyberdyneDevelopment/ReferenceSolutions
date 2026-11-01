using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceConnections.Endpoints.ConnectionEndpointOptions;

/// <summary>The TestConnection endpoint.</summary>
[WebMcpTool("test_connection", "Test connection.")]
[TypeOption(typeof(ConnectionEndpoints), "TestConnection")]
public class TestConnectionOption : ConnectionEndpointBase<TestConnectionEndpoint>
{
}
