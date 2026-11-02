using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataStores.Endpoints.DataStoreContainerEndpointOptions;

/// <summary>The GetDataStoreContainerFields endpoint.</summary>
[WebMcpTool("get_data_store_container_fields", "Get the fields of one data store container.", ReadOnly = true)]
[TypeOption(typeof(DataStoreContainerEndpoints), "GetDataStoreContainerFields")]
public class GetDataStoreContainerFieldsOption : DataStoreContainerEndpointBase<GetDataStoreContainerFieldsEndpoint>
{
}
