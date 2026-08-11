using Fdw.Collections.Attributes;

namespace ReferenceDataSets.Endpoints.DataSetEndpointOptions;

/// <summary>The DeleteDataSet endpoint.</summary>
[TypeOption(typeof(DataSetEndpoints), "DeleteDataSet")]
public class DeleteDataSetOption : DataSetEndpointBase<DeleteDataSetEndpoint>
{
}
