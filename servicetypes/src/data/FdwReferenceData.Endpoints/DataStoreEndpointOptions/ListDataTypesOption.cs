using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataStores.Endpoints.DataStoreEndpointOptions;

/// <summary>The ListDataTypes endpoint.</summary>
[WebMcpTool("list_data_types", "List the portable data types a container field's FrameworkDataType names.", ReadOnly = true)]
[TypeOption(typeof(DataStoreEndpoints), "ListDataTypes")]
public class ListDataTypesOption : DataStoreEndpointBase<ListDataTypesEndpoint>
{
}
