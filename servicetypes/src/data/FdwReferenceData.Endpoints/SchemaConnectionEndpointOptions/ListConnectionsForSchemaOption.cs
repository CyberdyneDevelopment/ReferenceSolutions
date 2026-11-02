using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSchema.Endpoints.SchemaConnectionEndpointOptions;

/// <summary>The ListConnectionsForSchema endpoint.</summary>
[WebMcpTool("list_connections_for_schema", "List connections for schema.", ReadOnly = true)]
[TypeOption(typeof(SchemaConnectionEndpoints), "ListConnectionsForSchema")]
public class ListConnectionsForSchemaOption : SchemaConnectionEndpointBase<ListConnectionsForSchemaEndpoint>
{
}
