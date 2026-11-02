using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataSets.Endpoints.DataSetEndpointOptions;

/// <summary>The LookupDataSets endpoint.</summary>
[WebMcpTool("lookup_data_sets", "Look up data sets by name.", ReadOnly = true)]
[TypeOption(typeof(DataSetEndpoints), "LookupDataSets")]
public class LookupDataSetsOption : DataSetEndpointBase<LookupDataSetsEndpoint>
{
}
