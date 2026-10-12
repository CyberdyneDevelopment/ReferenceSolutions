using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataStores.Endpoints.DataStoreEndpointOptions;

/// <summary>The ListDataStores endpoint.</summary>
[WebMcpTool("list_data_stores", "List data stores.", ReadOnly = true)]
[TypeOption(typeof(DataStoreEndpoints), "ListDataStores")]
public class ListDataStoresOption : DataStoreEndpointBase<ListDataStoresEndpoint>
{
}
