using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataStores.Endpoints.DataStorePathEndpointOptions;

/// <summary>The ListDataStorePathContainers endpoint.</summary>
[WebMcpTool("list_data_store_path_containers", "List the containers of a data store path.", ReadOnly = true)]
[TypeOption(typeof(DataStorePathEndpoints), "ListDataStorePathContainers")]
public class ListDataStorePathContainersOption : DataStorePathEndpointBase<ListDataStorePathContainersEndpoint>
{
}
