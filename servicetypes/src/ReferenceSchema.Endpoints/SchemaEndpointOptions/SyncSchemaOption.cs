using Fdw.Collections.Attributes;

namespace ReferenceSchema.Endpoints.SchemaEndpointOptions;

/// <summary>The SyncSchema endpoint.</summary>
[TypeOption(typeof(SchemaEndpoints), "SyncSchema")]
public class SyncSchemaOption : SchemaEndpointBase<SyncSchemaEndpoint>
{
}
