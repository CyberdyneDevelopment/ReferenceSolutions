using Fdw.Collections.Attributes;

namespace ReferenceDataStores.Endpoints.DataStoreContainerEndpointOptions;

/// <summary>The AddDataStoreContainer endpoint.</summary>
[TypeOption(typeof(DataStoreContainerEndpoints), "AddDataStoreContainer")]
public class AddDataStoreContainerOption : DataStoreContainerEndpointBase<AddDataStoreContainerEndpoint>
{
}
