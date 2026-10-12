using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceOperations.Endpoints.TypeCollectionEndpointOptions;

/// <summary>The ListTypeCollections endpoint.</summary>
[WebMcpTool("list_type_collections", "List the type collections this host loads.", ReadOnly = true)]
[TypeOption(typeof(TypeCollectionEndpoints), "ListTypeCollections")]
public class ListTypeCollectionsOption : TypeCollectionEndpointBase<ListTypeCollectionsEndpoint>
{
}
