using Fdw.Collections.Attributes;

namespace ReferenceSchema.Endpoints.SchemaEndpointOptions;

/// <summary>The DiscoverSchema endpoint.</summary>
[TypeOption(typeof(SchemaEndpoints), "DiscoverSchema")]
public class DiscoverSchemaOption : SchemaEndpointBase<DiscoverSchemaEndpoint>
{
}
