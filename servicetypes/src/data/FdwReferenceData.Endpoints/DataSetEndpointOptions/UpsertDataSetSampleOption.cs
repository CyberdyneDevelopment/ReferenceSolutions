using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataSets.Endpoints.DataSetEndpointOptions;

/// <summary>The UpsertDataSetSample endpoint.</summary>
[WebMcpTool("upsert_data_set_sample", "Replace a data set sample's rows.")]
[TypeOption(typeof(DataSetEndpoints), "UpsertDataSetSample")]
public class UpsertDataSetSampleOption : DataSetEndpointBase<UpsertDataSetSampleEndpoint>
{
}
