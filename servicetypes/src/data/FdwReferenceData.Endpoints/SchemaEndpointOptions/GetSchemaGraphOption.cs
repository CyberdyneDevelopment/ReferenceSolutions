using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSchema.Endpoints.SchemaEndpointOptions;

/// <summary>The GetSchemaGraph endpoint.</summary>
[WebMcpTool("get_schema_graph", "Get schema graph for ER diagram", ReadOnly = true)]
[TypeOption(typeof(SchemaEndpoints), "GetSchemaGraph")]
public class GetSchemaGraphOption : SchemaEndpointBase<GetSchemaGraphEndpoint>
{
}
