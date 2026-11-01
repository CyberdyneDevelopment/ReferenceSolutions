using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataStores.Endpoints.DataStoreContainerEndpointOptions;

/// <summary>The AddDataStoreContainer endpoint.</summary>
[WebMcpTool("add_data_store_container", "Add data store container.")]
[TypeOption(typeof(DataStoreContainerEndpoints), "AddDataStoreContainer")]
public class AddDataStoreContainerOption : DataStoreContainerEndpointBase<AddDataStoreContainerEndpoint>
{
}
