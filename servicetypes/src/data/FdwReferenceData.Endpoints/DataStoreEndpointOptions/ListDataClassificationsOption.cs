using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataStores.Endpoints.DataStoreEndpointOptions;

/// <summary>The ListDataClassifications endpoint.</summary>
[WebMcpTool("list_data_classifications", "List data classification levels.", ReadOnly = true)]
[TypeOption(typeof(DataStoreEndpoints), "ListDataClassifications")]
public class ListDataClassificationsOption : DataStoreEndpointBase<ListDataClassificationsEndpoint>
{
}
