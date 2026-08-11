using Fdw.Collections.Attributes;

namespace ReferenceSchema.Endpoints.SchemaEndpointOptions;

/// <summary>The RefreshSchema endpoint.</summary>
[TypeOption(typeof(SchemaEndpoints), "RefreshSchema")]
public class RefreshSchemaOption : SchemaEndpointBase<RefreshSchemaEndpoint>
{
}
