using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataStores.Endpoints.DataStoreEndpointOptions;

/// <summary>The UpdateDataStore endpoint.</summary>
[WebMcpTool("update_data_store", "Update data store.")]
[TypeOption(typeof(DataStoreEndpoints), "UpdateDataStore")]
public class UpdateDataStoreOption : DataStoreEndpointBase<UpdateDataStoreEndpoint>
{
}
