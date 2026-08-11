using Fdw.Collections.Attributes;

namespace ReferenceDataStores.Endpoints.DataStoreContainerEndpointOptions;

/// <summary>The GetContainer endpoint.</summary>
[TypeOption(typeof(DataStoreContainerEndpoints), "GetContainer")]
public class GetContainerOption : DataStoreContainerEndpointBase<GetContainerEndpoint>
{
}
