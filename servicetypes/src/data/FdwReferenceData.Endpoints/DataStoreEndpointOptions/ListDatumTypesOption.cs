using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataStores.Endpoints.DataStoreEndpointOptions;

/// <summary>The ListDatumTypes endpoint.</summary>
[WebMcpTool("list_datum_types", "List datum types: what a column's data is.", ReadOnly = true)]
[TypeOption(typeof(DataStoreEndpoints), "ListDatumTypes")]
public class ListDatumTypesOption : DataStoreEndpointBase<ListDatumTypesEndpoint>
{
}
