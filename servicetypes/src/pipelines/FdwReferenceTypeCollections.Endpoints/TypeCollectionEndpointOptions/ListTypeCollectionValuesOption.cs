using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceOperations.Endpoints.TypeCollectionEndpointOptions;

/// <summary>The ListTypeCollectionValues endpoint.</summary>
[WebMcpTool("list_type_collection_values", "List type collection values.", ReadOnly = true)]
[TypeOption(typeof(TypeCollectionEndpoints), "ListTypeCollectionValues")]
public class ListTypeCollectionValuesOption : TypeCollectionEndpointBase<ListTypeCollectionValuesEndpoint>
{
}
