using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataStores.Endpoints.DataStoreEndpointOptions;

/// <summary>The LookupDataStores endpoint.</summary>
[WebMcpTool("lookup_data_stores", "Look up data stores by name.", ReadOnly = true)]
[TypeOption(typeof(DataStoreEndpoints), "LookupDataStores")]
public class LookupDataStoresOption : DataStoreEndpointBase<LookupDataStoresEndpoint>
{
}
