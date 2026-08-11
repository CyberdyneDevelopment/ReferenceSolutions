using Fdw.Collections.Attributes;

namespace ReferenceDataSets.Endpoints.DataSetEndpointOptions;

/// <summary>The QueryDataSet endpoint.</summary>
[TypeOption(typeof(DataSetEndpoints), "QueryDataSet")]
public class QueryDataSetOption : DataSetEndpointBase<QueryDataSetEndpoint>
{
}
