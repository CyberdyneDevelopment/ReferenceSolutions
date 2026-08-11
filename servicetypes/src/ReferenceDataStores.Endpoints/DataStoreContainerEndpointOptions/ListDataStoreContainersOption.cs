using Fdw.Collections.Attributes;

namespace ReferenceDataStores.Endpoints.DataStoreContainerEndpointOptions;

/// <summary>The ListDataStoreContainers endpoint.</summary>
[TypeOption(typeof(DataStoreContainerEndpoints), "ListDataStoreContainers")]
public class ListDataStoreContainersOption : DataStoreContainerEndpointBase<ListDataStoreContainersEndpoint>
{
}
