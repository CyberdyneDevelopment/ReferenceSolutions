using Fdw.Collections.Attributes;

namespace ReferenceDataStores.Endpoints.DataStoreEndpointOptions;

/// <summary>The GetDataStore endpoint.</summary>
[TypeOption(typeof(DataStoreEndpoints), "GetDataStore")]
public class GetDataStoreOption : DataStoreEndpointBase<GetDataStoreEndpoint>
{
}
