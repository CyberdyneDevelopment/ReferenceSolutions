using Fdw.Collections.Attributes;

namespace ReferenceDataStores.Endpoints.DataStoreContainerEndpointOptions;

/// <summary>The GetContainerById endpoint.</summary>
[TypeOption(typeof(DataStoreContainerEndpoints), "GetContainerById")]
public class GetContainerByIdOption : DataStoreContainerEndpointBase<GetContainerByIdEndpoint>
{
}
