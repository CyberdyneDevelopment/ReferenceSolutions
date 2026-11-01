using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataStores.Endpoints.DataStoreContainerEndpointOptions;

/// <summary>The GetContainer endpoint.</summary>
[WebMcpTool("get_container", "Get container.", ReadOnly = true)]
[TypeOption(typeof(DataStoreContainerEndpoints), "GetContainer")]
public class GetContainerOption : DataStoreContainerEndpointBase<GetContainerEndpoint>
{
}
