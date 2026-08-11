using Fdw.Collections.Attributes;

namespace ReferenceDataStores.Endpoints.DataStorePathEndpointOptions;

/// <summary>The GetDataStorePaths endpoint.</summary>
[TypeOption(typeof(DataStorePathEndpoints), "GetDataStorePaths")]
public class GetDataStorePathsOption : DataStorePathEndpointBase<GetDataStorePathsEndpoint>
{
}
