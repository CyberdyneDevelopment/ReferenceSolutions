using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataSets.Endpoints.DataSetEndpointOptions;

/// <summary>The PreviewDataSet endpoint.</summary>
[WebMcpTool("preview_data_set", "Preview data set.", ReadOnly = true)]
[TypeOption(typeof(DataSetEndpoints), "PreviewDataSet")]
public class PreviewDataSetOption : DataSetEndpointBase<PreviewDataSetEndpoint>
{
}
