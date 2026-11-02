using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataStores.Endpoints.DataStoreContainerEndpointOptions;

/// <summary>The ListFormatTypes endpoint.</summary>
[WebMcpTool("list_format_types", "List container formats.", ReadOnly = true)]
[TypeOption(typeof(DataStoreContainerEndpoints), "ListFormatTypes")]
public class ListFormatTypesOption : DataStoreContainerEndpointBase<ListFormatTypesEndpoint>
{
}
