using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceConnections.Endpoints.ConnectionTypeEndpointOptions;

/// <summary>The ListConnectionTypeDefaultValuePolicies endpoint.</summary>
[WebMcpTool("list_connection_type_default_value_policies", "List the default-value policies a connection type registers.", ReadOnly = true)]
[TypeOption(typeof(ConnectionTypeEndpoints), "ListConnectionTypeDefaultValuePolicies")]
public class ListConnectionTypeDefaultValuePoliciesOption : ConnectionTypeEndpointBase<ListConnectionTypeDefaultValuePoliciesEndpoint>
{
}
