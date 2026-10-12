using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataSets.Endpoints.DataSetEndpointOptions;

/// <summary>The UpdateDataSet endpoint.</summary>
[WebMcpTool("update_data_set", "Update data set.")]
[TypeOption(typeof(DataSetEndpoints), "UpdateDataSet")]
public class UpdateDataSetOption : DataSetEndpointBase<UpdateDataSetEndpoint>
{
}
