using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSchema.Endpoints.SchemaEndpointOptions;

/// <summary>The SyncSchema endpoint.</summary>
[WebMcpTool("sync_schema", "Sync schema.")]
[TypeOption(typeof(SchemaEndpoints), "SyncSchema")]
public class SyncSchemaOption : SchemaEndpointBase<SyncSchemaEndpoint>
{
}
