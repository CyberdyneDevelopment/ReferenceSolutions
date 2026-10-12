using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSchema.Endpoints.SchemaEndpointOptions;

/// <summary>The DiscoverSchema endpoint.</summary>
[WebMcpTool("discover_schema", "Discover schema.")]
[TypeOption(typeof(SchemaEndpoints), "DiscoverSchema")]
public class DiscoverSchemaOption : SchemaEndpointBase<DiscoverSchemaEndpoint>
{
}
