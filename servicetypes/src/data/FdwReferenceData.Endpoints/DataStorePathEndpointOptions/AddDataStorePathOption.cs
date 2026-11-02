using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataStores.Endpoints.DataStorePathEndpointOptions;

/// <summary>The AddDataStorePath endpoint.</summary>
[WebMcpTool("add_data_store_path", "Add a path to a DataStore")]
[TypeOption(typeof(DataStorePathEndpoints), "AddDataStorePath")]
public class AddDataStorePathOption : DataStorePathEndpointBase<AddDataStorePathEndpoint>
{
}
