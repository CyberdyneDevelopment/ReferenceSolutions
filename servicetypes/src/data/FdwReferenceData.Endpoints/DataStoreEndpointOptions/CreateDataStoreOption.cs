using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataStores.Endpoints.DataStoreEndpointOptions;

/// <summary>The CreateDataStore endpoint.</summary>
[WebMcpTool("create_data_store", "Create data store.")]
[TypeOption(typeof(DataStoreEndpoints), "CreateDataStore")]
public class CreateDataStoreOption : DataStoreEndpointBase<CreateDataStoreEndpoint>
{
}
