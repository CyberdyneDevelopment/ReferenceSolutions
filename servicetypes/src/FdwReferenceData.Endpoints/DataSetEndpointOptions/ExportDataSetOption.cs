using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataSets.Endpoints.DataSetEndpointOptions;

/// <summary>The ExportDataSet endpoint.</summary>
[WebMcpTool("export_data_set", "Stream every row of a data set.", ReadOnly = true)]
[TypeOption(typeof(DataSetEndpoints), "ExportDataSet")]
public class ExportDataSetOption : DataSetEndpointBase<ExportDataSetEndpoint>
{
}
