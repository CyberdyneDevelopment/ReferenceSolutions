using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceOperations.Endpoints.TypeCollectionEndpointOptions;

/// <summary>The GetTypeCollectionValueDetail endpoint.</summary>
[WebMcpTool("get_type_collection_value_detail", "Get type collection value detail.", ReadOnly = true)]
[TypeOption(typeof(TypeCollectionEndpoints), "GetTypeCollectionValueDetail")]
public class GetTypeCollectionValueDetailOption : TypeCollectionEndpointBase<GetTypeCollectionValueDetailEndpoint>
{
}
