using Fdw.Collections.Attributes;

namespace ReferenceDataStores.Endpoints.DataStoreEndpointOptions;

/// <summary>The UpdateDataStore endpoint.</summary>
[TypeOption(typeof(DataStoreEndpoints), "UpdateDataStore")]
public class UpdateDataStoreOption : DataStoreEndpointBase<UpdateDataStoreEndpoint>
{
}
