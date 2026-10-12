using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataStores.Endpoints.DataStoreEndpointOptions;

/// <summary>The DiscoverDataStore endpoint.</summary>
[WebMcpTool("discover_data_store", "Discover data store.")]
[TypeOption(typeof(DataStoreEndpoints), "DiscoverDataStore")]
public class DiscoverDataStoreOption : DataStoreEndpointBase<DiscoverDataStoreEndpoint>
{
}
