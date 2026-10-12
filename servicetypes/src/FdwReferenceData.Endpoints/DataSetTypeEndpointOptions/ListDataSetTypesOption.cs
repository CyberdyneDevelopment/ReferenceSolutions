using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataSets.Endpoints.DataSetTypeEndpointOptions;

/// <summary>The ListDataSetTypes endpoint.</summary>
[WebMcpTool("list_data_set_types", "List data set types.", ReadOnly = true)]
[TypeOption(typeof(DataSetTypeEndpoints), "ListDataSetTypes")]
public class ListDataSetTypesOption : DataSetTypeEndpointBase<ListDataSetTypesEndpoint>
{
}
