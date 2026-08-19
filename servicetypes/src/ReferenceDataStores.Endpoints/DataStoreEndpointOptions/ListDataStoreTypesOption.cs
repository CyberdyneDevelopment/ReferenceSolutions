using Fdw.Collections.Attributes;

namespace ReferenceDataStores.Endpoints.DataStoreEndpointOptions;

/// <summary>The ListDataStoreTypes endpoint.</summary>
[TypeOption(typeof(DataStoreEndpoints), "ListDataStoreTypes")]
public class ListDataStoreTypesOption : DataStoreEndpointBase<ListDataStoreTypesEndpoint>
{
}
