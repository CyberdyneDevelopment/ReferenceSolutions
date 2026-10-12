using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataStores.Endpoints.DataStoreEndpointOptions;

/// <summary>The QueryDataStores endpoint.</summary>
[WebMcpTool("query_data_stores", "Query data stores by filter, order, page and field.", ReadOnly = true)]
[TypeOption(typeof(DataStoreEndpoints), "QueryDataStores")]
public class QueryDataStoresOption : DataStoreEndpointBase<QueryDataStoresEndpoint>
{
}
