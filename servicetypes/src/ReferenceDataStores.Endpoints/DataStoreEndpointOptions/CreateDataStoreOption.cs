using Fdw.Collections.Attributes;

namespace ReferenceDataStores.Endpoints.DataStoreEndpointOptions;

/// <summary>The CreateDataStore endpoint.</summary>
[TypeOption(typeof(DataStoreEndpoints), "CreateDataStore")]
public class CreateDataStoreOption : DataStoreEndpointBase<CreateDataStoreEndpoint>
{
}
