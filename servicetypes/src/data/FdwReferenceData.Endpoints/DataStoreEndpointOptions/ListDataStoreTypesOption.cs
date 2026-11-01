using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataStores.Endpoints.DataStoreEndpointOptions;

/// <summary>The ListDataStoreTypes endpoint.</summary>
[WebMcpTool("list_data_store_types", "List data store types.", ReadOnly = true)]
[TypeOption(typeof(DataStoreEndpoints), "ListDataStoreTypes")]
public class ListDataStoreTypesOption : DataStoreEndpointBase<ListDataStoreTypesEndpoint>
{
}
