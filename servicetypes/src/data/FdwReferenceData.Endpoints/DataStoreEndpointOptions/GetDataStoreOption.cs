using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataStores.Endpoints.DataStoreEndpointOptions;

/// <summary>The GetDataStore endpoint.</summary>
[WebMcpTool("get_data_store", "Get data store.", ReadOnly = true)]
[TypeOption(typeof(DataStoreEndpoints), "GetDataStore")]
public class GetDataStoreOption : DataStoreEndpointBase<GetDataStoreEndpoint>
{
}
