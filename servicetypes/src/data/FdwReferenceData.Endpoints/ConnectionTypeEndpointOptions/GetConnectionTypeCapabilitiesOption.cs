using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceConnections.Endpoints.ConnectionTypeEndpointOptions;

/// <summary>The GetConnectionTypeCapabilities endpoint.</summary>
[WebMcpTool("get_connection_type_capabilities", "Get connection type capabilities.", ReadOnly = true)]
[TypeOption(typeof(ConnectionTypeEndpoints), "GetConnectionTypeCapabilities")]
public class GetConnectionTypeCapabilitiesOption : ConnectionTypeEndpointBase<GetConnectionTypeCapabilitiesEndpoint>
{
}
