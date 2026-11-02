using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataSets.Endpoints.DataSetEndpointOptions;

/// <summary>The QueryDataSetData endpoint — POST /data/{DataSetName}.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("query_data_set_data", "Query rows of one data set the caller may read.", ReadOnly = true)]
[TypeOption(typeof(DataSetEndpoints), "QueryDataSetData")]
public class QueryDataSetDataOption : DataSetEndpointBase<QueryDataSetDataEndpoint>
{
}
