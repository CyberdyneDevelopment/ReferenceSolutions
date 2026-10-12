using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataStores.Endpoints.DataStorePathEndpointOptions;

/// <summary>The GetDataStorePaths endpoint.</summary>
[WebMcpTool("get_data_store_paths", "Get data store paths.", ReadOnly = true)]
[TypeOption(typeof(DataStorePathEndpoints), "GetDataStorePaths")]
public class GetDataStorePathsOption : DataStorePathEndpointBase<GetDataStorePathsEndpoint>
{
}
