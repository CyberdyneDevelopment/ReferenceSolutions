using Fdw.Collections.Attributes;

namespace ReferenceDataSets.Endpoints.DataSetEndpointOptions;

/// <summary>The ListDataSets endpoint.</summary>
[TypeOption(typeof(DataSetEndpoints), "ListDataSets")]
public class ListDataSetsOption : DataSetEndpointBase<ListDataSetsEndpoint>
{
}
