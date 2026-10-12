using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceConnections.Endpoints.ConnectionTypeEndpointOptions;

/// <summary>The ListConnectionTypes endpoint.</summary>
[WebMcpTool("list_connection_types", "List connection types.", ReadOnly = true)]
[TypeOption(typeof(ConnectionTypeEndpoints), "ListConnectionTypes")]
public class ListConnectionTypesOption : ConnectionTypeEndpointBase<ListConnectionTypesEndpoint>
{
}
