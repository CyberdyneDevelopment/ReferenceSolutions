using Fdw.Collections.Attributes;

namespace ReferenceDataSets.Endpoints.DataSetEndpointOptions;

/// <summary>The GetDataSet endpoint.</summary>
[TypeOption(typeof(DataSetEndpoints), "GetDataSet")]
public class GetDataSetOption : DataSetEndpointBase<GetDataSetEndpoint>
{
}
