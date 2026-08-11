using Fdw.Collections.Attributes;

namespace ReferenceSchema.Endpoints.SchemaConnectionEndpointOptions;

/// <summary>The ListConnectionsForSchema endpoint.</summary>
[TypeOption(typeof(SchemaConnectionEndpoints), "ListConnectionsForSchema")]
public class ListConnectionsForSchemaOption : SchemaConnectionEndpointBase<ListConnectionsForSchemaEndpoint>
{
}
