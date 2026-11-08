using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceConnections.Endpoints.ConnectionEndpointOptions;

/// <summary>The EnableConnection endpoint.</summary>
[WebMcpTool("enable_connection", "Enable a disabled connection and record why.")]
[TypeOption(typeof(ConnectionEndpoints), "EnableConnection")]
public class EnableConnectionOption : ConnectionEndpointBase<EnableConnectionEndpoint>
{
}
