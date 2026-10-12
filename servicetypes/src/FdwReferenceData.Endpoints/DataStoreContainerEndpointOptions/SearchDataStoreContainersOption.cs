using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataStores.Endpoints.DataStoreContainerEndpointOptions;

/// <summary>The SearchDataStoreContainers endpoint.</summary>
[WebMcpTool("search_data_store_containers", "Find the containers of a data store.", ReadOnly = true)]
[TypeOption(typeof(DataStoreContainerEndpoints), "SearchDataStoreContainers")]
public class SearchDataStoreContainersOption : DataStoreContainerEndpointBase<SearchDataStoreContainersEndpoint>
{
}
