using Fdw.Collections.Attributes;

namespace ReferenceDataStores.Endpoints.DataStorePathEndpointOptions;

/// <summary>The AddDataStorePath endpoint.</summary>
[TypeOption(typeof(DataStorePathEndpoints), "AddDataStorePath")]
public class AddDataStorePathOption : DataStorePathEndpointBase<AddDataStorePathEndpoint>
{
}
