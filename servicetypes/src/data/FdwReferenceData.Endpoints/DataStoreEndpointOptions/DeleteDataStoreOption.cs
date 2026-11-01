using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataStores.Endpoints.DataStoreEndpointOptions;

/// <summary>The DeleteDataStore endpoint.</summary>
[WebMcpTool("delete_data_store", "Delete data store.")]
[TypeOption(typeof(DataStoreEndpoints), "DeleteDataStore")]
public class DeleteDataStoreOption : DataStoreEndpointBase<DeleteDataStoreEndpoint>
{
}
