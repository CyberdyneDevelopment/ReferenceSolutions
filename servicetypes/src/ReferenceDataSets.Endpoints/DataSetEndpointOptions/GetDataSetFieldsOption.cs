using Fdw.Collections.Attributes;

namespace ReferenceDataSets.Endpoints.DataSetEndpointOptions;

/// <summary>The GetDataSetFields endpoint.</summary>
[TypeOption(typeof(DataSetEndpoints), "GetDataSetFields")]
public class GetDataSetFieldsOption : DataSetEndpointBase<GetDataSetFieldsEndpoint>
{
}
