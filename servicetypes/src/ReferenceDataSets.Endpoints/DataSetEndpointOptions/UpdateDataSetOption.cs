using Fdw.Collections.Attributes;

namespace ReferenceDataSets.Endpoints.DataSetEndpointOptions;

/// <summary>The UpdateDataSet endpoint.</summary>
[TypeOption(typeof(DataSetEndpoints), "UpdateDataSet")]
public class UpdateDataSetOption : DataSetEndpointBase<UpdateDataSetEndpoint>
{
}
