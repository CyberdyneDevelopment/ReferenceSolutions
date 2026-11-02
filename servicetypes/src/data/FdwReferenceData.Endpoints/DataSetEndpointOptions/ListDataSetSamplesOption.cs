using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataSets.Endpoints.DataSetEndpointOptions;

/// <summary>The ListDataSetSamples endpoint.</summary>
[WebMcpTool("list_data_set_samples", "List a data set's samples.", ReadOnly = true)]
[TypeOption(typeof(DataSetEndpoints), "ListDataSetSamples")]
public class ListDataSetSamplesOption : DataSetEndpointBase<ListDataSetSamplesEndpoint>
{
}
