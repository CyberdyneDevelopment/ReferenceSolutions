using Fdw.Collections.Attributes;

namespace ReferenceDataSets.Endpoints.DataSetEndpointOptions;

/// <summary>The CreateDataSet endpoint.</summary>
[TypeOption(typeof(DataSetEndpoints), "CreateDataSet")]
public class CreateDataSetOption : DataSetEndpointBase<CreateDataSetEndpoint>
{
}
