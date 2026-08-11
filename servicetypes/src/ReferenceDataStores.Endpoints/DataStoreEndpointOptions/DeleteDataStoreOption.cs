using Fdw.Collections.Attributes;

namespace ReferenceDataStores.Endpoints.DataStoreEndpointOptions;

/// <summary>The DeleteDataStore endpoint.</summary>
[TypeOption(typeof(DataStoreEndpoints), "DeleteDataStore")]
public class DeleteDataStoreOption : DataStoreEndpointBase<DeleteDataStoreEndpoint>
{
}
