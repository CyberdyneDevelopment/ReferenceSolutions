using Fdw.Collections.Attributes;

namespace ReferenceDataStores.Endpoints.DataStoreEndpointOptions;

/// <summary>The ListDataStores endpoint.</summary>
[TypeOption(typeof(DataStoreEndpoints), "ListDataStores")]
public class ListDataStoresOption : DataStoreEndpointBase<ListDataStoresEndpoint>
{
}
