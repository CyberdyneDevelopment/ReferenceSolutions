using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataSets.Endpoints.DataSetEndpointOptions;

/// <summary>The CreateDataSet endpoint.</summary>
[WebMcpTool("create_data_set", "Create data set.")]
[TypeOption(typeof(DataSetEndpoints), "CreateDataSet")]
public class CreateDataSetOption : DataSetEndpointBase<CreateDataSetEndpoint>
{
}
