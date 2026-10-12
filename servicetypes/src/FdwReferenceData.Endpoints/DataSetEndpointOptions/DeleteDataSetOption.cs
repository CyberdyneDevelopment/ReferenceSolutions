using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataSets.Endpoints.DataSetEndpointOptions;

/// <summary>The DeleteDataSet endpoint.</summary>
[WebMcpTool("delete_data_set", "Delete data set.")]
[TypeOption(typeof(DataSetEndpoints), "DeleteDataSet")]
public class DeleteDataSetOption : DataSetEndpointBase<DeleteDataSetEndpoint>
{
}
