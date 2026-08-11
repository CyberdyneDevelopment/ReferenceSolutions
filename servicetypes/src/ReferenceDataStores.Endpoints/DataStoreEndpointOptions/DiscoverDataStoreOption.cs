using Fdw.Collections.Attributes;

namespace ReferenceDataStores.Endpoints.DataStoreEndpointOptions;

/// <summary>The DiscoverDataStore endpoint.</summary>
[TypeOption(typeof(DataStoreEndpoints), "DiscoverDataStore")]
public class DiscoverDataStoreOption : DataStoreEndpointBase<DiscoverDataStoreEndpoint>
{
}
