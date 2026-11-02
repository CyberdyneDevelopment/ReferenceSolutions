using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataStores.Endpoints.DataStoreContainerEndpointOptions;

/// <summary>The ListDataStoreContainers endpoint.</summary>
[WebMcpTool("list_data_store_containers", "List data store containers.", ReadOnly = true)]
[TypeOption(typeof(DataStoreContainerEndpoints), "ListDataStoreContainers")]
public class ListDataStoreContainersOption : DataStoreContainerEndpointBase<ListDataStoreContainersEndpoint>
{
}
