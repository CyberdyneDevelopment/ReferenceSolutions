using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSchema.Endpoints.SchemaEndpointOptions;

/// <summary>The RefreshSchema endpoint.</summary>
[WebMcpTool("refresh_schema", "Refresh schema.")]
[TypeOption(typeof(SchemaEndpoints), "RefreshSchema")]
public class RefreshSchemaOption : SchemaEndpointBase<RefreshSchemaEndpoint>
{
}
