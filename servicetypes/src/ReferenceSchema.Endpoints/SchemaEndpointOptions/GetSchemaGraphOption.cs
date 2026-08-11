using Fdw.Collections.Attributes;

namespace ReferenceSchema.Endpoints.SchemaEndpointOptions;

/// <summary>The GetSchemaGraph endpoint.</summary>
[TypeOption(typeof(SchemaEndpoints), "GetSchemaGraph")]
public class GetSchemaGraphOption : SchemaEndpointBase<GetSchemaGraphEndpoint>
{
}
